using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.Grpc.Security;
using Portfolio.Infrastructure.Helpers;
using SkiaSharp;

namespace Portfolio.Grpc.Services;

public readonly record struct HeroImageResult(byte[]? Bytes, string Version, bool IsStale);

public class HeroImageProcessor(
    HttpClient httpClient,
    SafeUrlValidator urlValidator,
    IMemoryCache memoryCache,
    ILogger<HeroImageProcessor> logger)
{
    private const int DesktopWidth = 1920;
    private const int DesktopHeight = 1080;

    private const int MobileWidth = 768;
    private const int MobileHeight = 1280;

    private const long MaxDownloadBytes = 10 * 1024 * 1024;
    private const int MaxDimensionPixels = 4096;
    private const int MaxRedirects = 5;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(23);
    private static readonly SemaphoreSlim DesktopLock = new(1, 1);
    private static readonly SemaphoreSlim MobileLock = new(1, 1);

    private sealed record StaleImageEntry(byte[] Bytes, string Version);

    private static SemaphoreSlim GetLock(string variant) =>
        string.Equals(variant, DeviceTypes.Mobile, StringComparison.OrdinalIgnoreCase) ? MobileLock : DesktopLock;

    public virtual async Task<HeroImageResult> GetEncodedImageAsync(
        string? imageUrl,
        string? variant,
        CancellationToken cancellationToken = default)
    {
        var normalizedVariant = string.Equals(variant, DeviceTypes.Mobile, StringComparison.OrdinalIgnoreCase)
            ? "mobile"
            : "desktop";

        var version = ImageVersionHelper.ComputeVersionHash(imageUrl);

        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return new HeroImageResult(null, version, false);

        var bytes = await EnsureVariantCachedAsync(imageUrl, version, normalizedVariant, cancellationToken);
        if (bytes is { Length: > 0 })
            return new HeroImageResult(bytes, version, false);

        if (memoryCache.TryGetValue(GetStaleCacheKey(normalizedVariant), out StaleImageEntry? staleEntry) &&
            staleEntry?.Bytes is { Length: > 0 })
        {
            logger.LogWarning("Serving stale {Variant} WebP bytes as fallback (version {Version})", normalizedVariant, staleEntry.Version);
            return new HeroImageResult(staleEntry.Bytes, staleEntry.Version, true);
        }

        return new HeroImageResult(null, version, false);
    }

    private static string GetBytesCacheKey(string normalizedVariant, string version) =>
        $"hero_image_bytes_{normalizedVariant}_{version}";

    private static string GetStaleCacheKey(string normalizedVariant) =>
        $"hero_image_bytes_{normalizedVariant}_stale";

    private async Task<byte[]?> EnsureVariantCachedAsync(
        string imageUrl,
        string version,
        string normalizedVariant,
        CancellationToken cancellationToken)
    {
        var bytesCacheKey = GetBytesCacheKey(normalizedVariant, version);
        if (memoryCache.TryGetValue(bytesCacheKey, out byte[]? cachedBytes) && cachedBytes is { Length: > 0 })
            return cachedBytes;

        var variantLock = GetLock(normalizedVariant);
        await variantLock.WaitAsync(cancellationToken);
        try
        {
            if (memoryCache.TryGetValue(bytesCacheKey, out cachedBytes) && cachedBytes is { Length: > 0 })
                return cachedBytes;

            var encodedBytes = await DownloadAndConvertToWebpAsync(imageUrl, normalizedVariant, cancellationToken);
            if (encodedBytes is { Length: > 0 })
            {
                memoryCache.Set(bytesCacheKey, encodedBytes, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheDuration
                });
                memoryCache.Set(GetStaleCacheKey(normalizedVariant), new StaleImageEntry(encodedBytes, version));
                logger.LogInformation(
                    "Cached {Variant} WebP hero image ({SizeBytes} bytes, version {Version})",
                    normalizedVariant,
                    encodedBytes.Length,
                    version);
                return encodedBytes;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to download or encode {Variant} hero image from {Url}", normalizedVariant, imageUrl);
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
            if (!await urlValidator.IsSafePublicUrlAsync(url, cancellationToken))
            {
                logger.LogWarning("Rejected hero image download from unsafe URL '{Url}'", url);
                return null;
            }

            var currentUri = new Uri(url, UriKind.Absolute);

            for (var hop = 0; hop <= MaxRedirects; hop++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (IsRedirectStatusCode(response.StatusCode))
                {
                    if (hop == MaxRedirects)
                    {
                        logger.LogWarning("Hero image download exceeded max redirects ({MaxRedirects}) for URL '{Url}'", MaxRedirects, url);
                        return null;
                    }

                    var location = response.Headers.Location;
                    if (location is null)
                        return null;

                    var nextUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
                    if (!await urlValidator.IsSafePublicUrlAsync(nextUri.AbsoluteUri, cancellationToken))
                    {
                        logger.LogWarning("Blocked unsafe redirect during hero image download from '{SourceUrl}' to '{TargetUrl}'", currentUri, nextUri);
                        return null;
                    }

                    currentUri = nextUri;
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                    return null;

                if (response.Content.Headers.ContentLength is > MaxDownloadBytes)
                {
                    logger.LogWarning(
                        "Rejected hero image download from {Url}: Content-Length {ContentLength} exceeds {MaxDownloadBytes} bytes",
                        currentUri,
                        response.Content.Headers.ContentLength,
                        MaxDownloadBytes);
                    return null;
                }

                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (!string.IsNullOrEmpty(mediaType) && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "Rejected hero image download from {Url}: unexpected Content-Type {MediaType}",
                        currentUri,
                        mediaType);
                    return null;
                }

                var bytes = await ReadContentWithLimitAsync(response.Content, MaxDownloadBytes, cancellationToken);
                if (bytes is null)
                {
                    logger.LogWarning(
                        "Rejected hero image download from {Url}: response body exceeded {MaxDownloadBytes} bytes",
                        currentUri,
                        MaxDownloadBytes);
                    return null;
                }

                return bytes;
            }

            return null;
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

    private static async Task<byte[]?> ReadContentWithLimitAsync(HttpContent content, long maxBytes, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var memoryStream = new MemoryStream();
        var buffer = new byte[81920];
        long totalRead = 0;

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            totalRead += bytesRead;
            if (totalRead > maxBytes)
                return null;

            memoryStream.Write(buffer, 0, bytesRead);
        }

        return memoryStream.ToArray();
    }

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

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
