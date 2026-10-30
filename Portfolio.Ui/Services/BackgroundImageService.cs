using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.Infrastructure.Constants;
using Portfolio.Infrastructure.Extensions;
using Portfolio.Ui.Models;
using SkiaSharp;

namespace Portfolio.Ui.Services;

public class BackgroundImageService(BackgroundImages.BackgroundImagesClient backgroundImagesClient, IMemoryCache memoryCache, ILogger<BackgroundImageService> logger, HttpClient httpClient)
{
    private const string FallbackBackgroundImage = "img/default-bg.webp";
    private const string FallbackAltText = "A lush, tropical landscape with a dramatic view of a conical mountain in the distance, possibly a volcano, surrounded by layers of rolling hills and dense forests. The sky above features a stunning mix of colors and clouds.";
    private const string DefaultBingAltText = "Daily featured background photograph from Bing Wallpaper";

    private const string CacheKey = "background_image";
    private const int DesktopWidth = 1920;
    private const int DesktopHeight = 1080;

    private const int MobileWidth = 768;
    private const int MobileHeight = 1280;

    private const long MaxDownloadBytes = 10 * 1024 * 1024;
    private const int MaxDimensionPixels = 4096;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(23);
    private static readonly SemaphoreSlim DesktopLock = new(1, 1);
    private static readonly SemaphoreSlim MobileLock = new(1, 1);

    private static SemaphoreSlim GetLock(string variant) => string.Equals(variant, DeviceTypes.Mobile, StringComparison.OrdinalIgnoreCase) ? MobileLock : DesktopLock;

    private async Task<BackgroundImageModel> GetFromDbAsync(CancellationToken cancellationToken = default)
    {
        if (EnvVars.IsDevelopment())
            return new() { Url = FallbackBackgroundImage, AltText = FallbackAltText, Source = ImageOfTheDaySource.Bing };

        var response = await backgroundImagesClient.GetAsync(new Empty(), cancellationToken: cancellationToken);
        logger.LogInformation("📨 Sent a gRPC request to {ServiceName}, ResponseUrl: {ResponseUrl}", nameof(backgroundImagesClient.GetAsync), response.Url);

        if (response.Source == ImageOfTheDaySource.None || string.IsNullOrWhiteSpace(response.Url))
        {
            logger.LogError("❌ There is no image of the day available at gRPC, serving default bg img.");
            return new()
            {
                Url = FallbackBackgroundImage,
                AltText = FallbackAltText,
                Source = ImageOfTheDaySource.Bing
            };
        }

        return new()
        {
            Url = response.Url,
            AltText = string.IsNullOrWhiteSpace(response.AltText) ? DefaultBingAltText : response.AltText,
            Source = response.Source
        };
    }

    public virtual async Task<BackgroundImageModel> GetFromCacheAsync(CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(CacheKey, out BackgroundImageModel? cachedImage) && cachedImage != null)
        {
            logger.LogInformation("📦 Background image retrieved from cache");
            return cachedImage;
        }

        var image = await GetFromDbAsync(cancellationToken);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        };

        memoryCache.Set(CacheKey, image, cacheOptions);
        logger.LogInformation("💾 Background image added to cache with 23-hour expiration");

        return image;
    }

    public virtual async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        BackgroundImageModel latestImage;
        try
        {
            latestImage = await GetFromDbAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve background image details from gRPC during background refresh");
            if (memoryCache.TryGetValue(CacheKey, out BackgroundImageModel? existingImage) && existingImage != null)
                return;

            throw;
        }

        if (latestImage.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            // Pre-download and encode WebP for both variants under latestImage.Version
            await EnsureVariantCachedAsync(latestImage, "desktop", cancellationToken);
            await EnsureVariantCachedAsync(latestImage, "mobile", cancellationToken);
        }

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        };

        memoryCache.Set(CacheKey, latestImage, cacheOptions);
        logger.LogInformation("🔄 Background image metadata and WebP cache refreshed for version {Version}", latestImage.Version);
    }

    public async Task WarmupCacheAsync(CancellationToken cancellationToken = default)
    {
        await RefreshCacheAsync(cancellationToken);
    }

    public virtual async Task<(byte[]? Bytes, bool IsStale)> GetHeroImageBytesAsync(string variant, CancellationToken cancellationToken = default)
    {
        var normalizedVariant = string.Equals(variant, DeviceTypes.Mobile, StringComparison.OrdinalIgnoreCase) ? "mobile" : "desktop";

        var image = await GetFromCacheAsync(cancellationToken);
        var bytes = await EnsureVariantCachedAsync(image, normalizedVariant, cancellationToken);
        if (bytes is { Length: > 0 })
            return (bytes, false);

        // Stale fallback: if current download/encode failed, check if previously cached bytes exist
        if (memoryCache.TryGetValue($"background_image_bytes_{normalizedVariant}_stale", out byte[]? staleBytes) && staleBytes is { Length: > 0 })
        {
            logger.LogWarning("⚠️ Serving stale {Variant} WebP bytes as fallback", normalizedVariant);
            return (staleBytes, true);
        }

        return (null, false);
    }

    private async Task<byte[]?> EnsureVariantCachedAsync(BackgroundImageModel image, string variant, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(image.Url) || !image.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return null;

        var normalizedVariant = string.Equals(variant, DeviceTypes.Mobile, StringComparison.OrdinalIgnoreCase) ? "mobile" : "desktop";
        var bytesCacheKey = $"background_image_bytes_{normalizedVariant}_{image.Version}";
        if (memoryCache.TryGetValue(bytesCacheKey, out byte[]? cachedBytes) && cachedBytes is { Length: > 0 })
            return cachedBytes;

        var variantLock = GetLock(normalizedVariant);
        await variantLock.WaitAsync(cancellationToken);
        try
        {
            if (memoryCache.TryGetValue(bytesCacheKey, out cachedBytes) && cachedBytes is { Length: > 0 })
                return cachedBytes;

            var encodedBytes = await DownloadAndConvertToWebpAsync(image.Url, normalizedVariant, cancellationToken);
            if (encodedBytes is { Length: > 0 })
            {
                memoryCache.Set(bytesCacheKey, encodedBytes, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheDuration
                });
                memoryCache.Set($"background_image_bytes_{normalizedVariant}_stale", encodedBytes);
                logger.LogInformation("🖼️ Cached {Variant} WebP hero image ({SizeBytes} bytes)", normalizedVariant, encodedBytes.Length);
                return encodedBytes;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to download or encode {Variant} hero image from {Url}", normalizedVariant, image.Url);
        }
        finally
        {
            variantLock.Release();
        }

        return null;
    }

    private async Task<byte[]?> DownloadAndConvertToWebpAsync(string url, string variant, CancellationToken cancellationToken)
    {
        var isMobile = string.Equals(variant, DeviceTypes.Mobile, StringComparison.OrdinalIgnoreCase);
        if (isMobile && url.Contains("_1920x1080.jpg", StringComparison.OrdinalIgnoreCase))
        {
            var bingMobileUrl = url.Replace("_1920x1080.jpg", "_768x1280.jpg", StringComparison.OrdinalIgnoreCase);
            var mobileBytes = await TryDownloadBytesAsync(bingMobileUrl, cancellationToken);
            if (mobileBytes is { Length: > 0 })
            {
                return EncodeWebp(mobileBytes, MobileWidth, MobileHeight, cropToFill: false, quality: 75);
            }
        }

        var sourceBytes = await TryDownloadBytesAsync(url, cancellationToken);
        if (sourceBytes is not { Length: > 0 })
            return null;

        return isMobile
            ? EncodeWebp(sourceBytes, MobileWidth, MobileHeight, cropToFill: true, quality: 75)
            : EncodeWebp(sourceBytes, DesktopWidth, DesktopHeight, cropToFill: false, quality: 80);
    }

    private async Task<byte[]?> TryDownloadBytesAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            if (response.Content.Headers.ContentLength is > MaxDownloadBytes)
            {
                logger.LogWarning("Rejected hero image download from {Url}: Content-Length {ContentLength} exceeds {MaxDownloadBytes} bytes", url, response.Content.Headers.ContentLength, MaxDownloadBytes);
                return null;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!string.IsNullOrEmpty(mediaType) && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Rejected hero image download from {Url}: unexpected Content-Type {MediaType}", url, mediaType);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return bytes.Length <= MaxDownloadBytes ? bytes : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "HTTP download failed for hero image URL {Url}", url);
            return null;
        }
    }

    private static byte[]? EncodeWebp(
        byte[] sourceBytes,
        int targetWidth,
        int targetHeight,
        bool cropToFill,
        int quality)
    {
        using var original = SKBitmap.Decode(sourceBytes);
        if (original == null ||
            original.Width <= 0 ||
            original.Height <= 0 ||
            original.Width > MaxDimensionPixels ||
            original.Height > MaxDimensionPixels)
            return null;

        if (!cropToFill && original.Width <= targetWidth && original.Height <= targetHeight)
        {
            using var directImage = SKImage.FromBitmap(original);
            using var directData = directImage.Encode(SKEncodedImageFormat.Webp, quality);
            return directData.ToArray();
        }

        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        if (!cropToFill)
        {
            var scale = Math.Min((float)targetWidth / original.Width, (float)targetHeight / original.Height);
            var scaledWidth = Math.Max(1, (int)Math.Round(original.Width * scale));
            var scaledHeight = Math.Max(1, (int)Math.Round(original.Height * scale));

            using var resized = original.Resize(new SKImageInfo(scaledWidth, scaledHeight), sampling);
            if (resized == null)
                return null;

            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
            return data.ToArray();
        }

        var cropScale = Math.Max((float)targetWidth / original.Width, (float)targetHeight / original.Height);
        var cropWidth = Math.Min(original.Width, Math.Max(1, (int)Math.Round(targetWidth / cropScale)));
        var cropHeight = Math.Min(original.Height, Math.Max(1, (int)Math.Round(targetHeight / cropScale)));
        var cropLeft = (original.Width - cropWidth) / 2;
        var cropTop = (original.Height - cropHeight) / 2;

        using var subset = new SKBitmap();
        if (!original.ExtractSubset(subset, new SKRectI(cropLeft, cropTop, cropLeft + cropWidth, cropTop + cropHeight)))
            return null;

        if (subset.Width <= targetWidth && subset.Height <= targetHeight)
        {
            using var directCroppedImage = SKImage.FromBitmap(subset);
            using var directCroppedData = directCroppedImage.Encode(SKEncodedImageFormat.Webp, quality);
            return directCroppedData.ToArray();
        }

        using var croppedResized = subset.Resize(new SKImageInfo(targetWidth, targetHeight), sampling);
        if (croppedResized == null)
            return null;

        using var croppedImage = SKImage.FromBitmap(croppedResized);
        using var croppedData = croppedImage.Encode(SKEncodedImageFormat.Webp, quality);
        return croppedData.ToArray();
    }
}
