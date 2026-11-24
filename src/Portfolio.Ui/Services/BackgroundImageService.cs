using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.Infrastructure.Constants;
using Portfolio.Infrastructure.Extensions;
using Portfolio.Ui.Models;

namespace Portfolio.Ui.Services;

public class BackgroundImageService(BackgroundImages.BackgroundImagesClient backgroundImagesClient, IMemoryCache memoryCache, ILogger<BackgroundImageService> logger)
{
    private const string FallbackBackgroundImage = "img/default-bg.webp";
    private const string FallbackAltText = "A lush, tropical landscape with a dramatic view of a conical mountain in the distance, possibly a volcano, surrounded by layers of rolling hills and dense forests. The sky above features a stunning mix of colors and clouds.";
    private const string DefaultBingAltText = "Daily featured background photograph from Bing Wallpaper";

    private const string CacheKey = "background_image";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(23);
    private static readonly TimeSpan FallbackCacheDuration = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan GrpcTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan GrpcImageTimeout = TimeSpan.FromSeconds(15);
    private static readonly SemaphoreSlim CacheLock = new(1, 1);
    private static readonly SemaphoreSlim DesktopLock = new(1, 1);
    private static readonly SemaphoreSlim MobileLock = new(1, 1);

    private static SemaphoreSlim GetLock(string variant) => string.Equals(variant, DeviceTypes.Mobile, StringComparison.OrdinalIgnoreCase) ? MobileLock : DesktopLock;

    private static BackgroundImageModel CreateFallbackImage() => new()
    {
        Url = FallbackBackgroundImage,
        AltText = FallbackAltText,
        Source = ImageOfTheDaySource.Bing
    };

    private async Task<BackgroundImageModel> GetFromDbAsync(CancellationToken cancellationToken = default)
    {
        if (EnvVars.IsDevelopment())
            return CreateFallbackImage();

        var deadline = DateTime.UtcNow.Add(GrpcTimeout);
        var response = await backgroundImagesClient.GetAsync(new Empty(), deadline: deadline, cancellationToken: cancellationToken);
        logger.LogInformation("📨 Sent a gRPC request to {ServiceName}, ResponseUrl: {ResponseUrl}", nameof(backgroundImagesClient.GetAsync), response.Url);

        if (response.Source == ImageOfTheDaySource.None || string.IsNullOrWhiteSpace(response.Url))
        {
            logger.LogError("❌ There is no image of the day available at gRPC, serving default bg img.");
            return CreateFallbackImage();
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

        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            if (memoryCache.TryGetValue(CacheKey, out cachedImage) && cachedImage != null)
            {
                logger.LogInformation("📦 Background image retrieved from cache");
                return cachedImage;
            }

            return await memoryCache.GetOrCreateAsync(CacheKey, async entry =>
            {
                try
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                    var image = await GetFromDbAsync(cancellationToken);
                    logger.LogInformation("💾 Background image added to cache with 23-hour expiration");
                    return image;
                }
                catch (RpcException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "⚠️ gRPC backend unavailable when loading background image; serving fallback image");
                    entry.AbsoluteExpirationRelativeToNow = FallbackCacheDuration;
                    return CreateFallbackImage();
                }
            }) ?? throw new InvalidOperationException("Failed to retrieve background image from cache.");
        }
        finally
        {
            CacheLock.Release();
        }
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
        var (bytes, isStale) = await EnsureVariantCachedAsync(image, normalizedVariant, cancellationToken);
        if (bytes is { Length: > 0 })
            return (bytes, isStale);

        // Stale fallback: if gRPC fetch failed, check if previously cached bytes exist
        if (memoryCache.TryGetValue($"background_image_bytes_{normalizedVariant}_stale", out byte[]? staleBytes) && staleBytes is { Length: > 0 })
        {
            logger.LogWarning("⚠️ Serving stale {Variant} WebP bytes as fallback", normalizedVariant);
            return (staleBytes, true);
        }

        return (null, false);
    }

    private async Task<(byte[]? Bytes, bool IsStale)> EnsureVariantCachedAsync(BackgroundImageModel image, string variant, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(image.Url) || !image.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return (null, false);

        var normalizedVariant = string.Equals(variant, DeviceTypes.Mobile, StringComparison.OrdinalIgnoreCase) ? "mobile" : "desktop";
        var bytesCacheKey = $"background_image_bytes_{normalizedVariant}_{image.Version}";
        if (memoryCache.TryGetValue(bytesCacheKey, out byte[]? cachedBytes) && cachedBytes is { Length: > 0 })
            return (cachedBytes, false);

        var variantLock = GetLock(normalizedVariant);
        await variantLock.WaitAsync(cancellationToken);
        try
        {
            if (memoryCache.TryGetValue(bytesCacheKey, out cachedBytes) && cachedBytes is { Length: > 0 })
                return (cachedBytes, false);

            var deadline = DateTime.UtcNow.Add(GrpcImageTimeout);
            var response = await backgroundImagesClient.GetImageBytesAsync(
                new HeroImageRequest { Variant = normalizedVariant },
                deadline: deadline,
                cancellationToken: cancellationToken);

            if (response.Data is { Length: > 0 })
            {
                var encodedBytes = response.Data.ToByteArray();
                memoryCache.Set($"background_image_bytes_{normalizedVariant}_stale", encodedBytes);

                if (response.IsStale)
                {
                    logger.LogWarning("⚠️ gRPC returned stale {Variant} WebP hero image ({SizeBytes} bytes)", normalizedVariant, encodedBytes.Length);
                    return (encodedBytes, true);
                }

                memoryCache.Set(bytesCacheKey, encodedBytes, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheDuration
                });
                logger.LogInformation("🖼️ Cached {Variant} WebP hero image from gRPC ({SizeBytes} bytes)", normalizedVariant, encodedBytes.Length);
                return (encodedBytes, false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch {Variant} hero image bytes from gRPC", normalizedVariant);
        }
        finally
        {
            variantLock.Release();
        }

        return (null, false);
    }
}
