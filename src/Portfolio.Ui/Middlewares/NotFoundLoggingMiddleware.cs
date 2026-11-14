using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.Infrastructure.Helpers;
using Portfolio.Ui.Extensions;
using Portfolio.Ui.Services;

namespace Portfolio.Ui.Middlewares;

public class NotFoundLoggingMiddleware(
    RequestDelegate next,
    ILogger<NotFoundLoggingMiddleware> logger,
    IMemoryCache memoryCache)
{
    internal const int Max404LogsPerIpPerWindow = 10;
    internal static readonly TimeSpan CacheWindow = TimeSpan.FromMinutes(10);
    private const int MaxRawPathKeyLength = 256;

    public async Task InvokeAsync(HttpContext context, LogVisitService logVisitService, IHttpContextAccessor httpContextAccessor)
    {
        await next(context);

        // Skip duplicate logging if this is an internal re-execution from StatusCodePagesWithReExecute
        if (context.Features.Get<IStatusCodeReExecuteFeature>() != null)
            return;

        if (context.Response.StatusCode != StatusCodes.Status404NotFound)
            return;

        var path = context.Request.Path.Value ?? string.Empty;
        if (!string.IsNullOrEmpty(path) && AssetHelper.IsStaticAsset(path))
        {
            logger.LogDebug("Skipping 404 visit logging for static asset: {RequestedUrl}", path);
            return;
        }

        var clientIp = context.GetClientIpAddress();
        if (!TryAcquireLogSlot(clientIp, path))
        {
            logger.LogDebug("Skipping duplicate or throttled 404 visit logging for {ClientIp} on {RequestedUrl}", clientIp, path);
            return;
        }

        logger.LogInformation("🚫 Not Found: {RequestedUrl}", path);

        try
        {
            await logVisitService.LogVisitToWebpageAsync(httpContextAccessor, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // Request was aborted by the client, ignore
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log visit for 404 response");
        }
    }

    private bool TryAcquireLogSlot(string clientIp, string path)
    {
        var dedupKey = BuildDedupKey(clientIp, path);
        if (memoryCache.TryGetValue(dedupKey, out _))
        {
            return false;
        }

        var ipCountKey = $"404_ip_count:{clientIp}";
        var counter = memoryCache.GetOrCreate(ipCountKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheWindow;
            return new IpNotFoundCounter();
        })!;

        if (Volatile.Read(ref counter.Count) >= Max404LogsPerIpPerWindow)
            return false;

        if (Interlocked.Increment(ref counter.Count) > Max404LogsPerIpPerWindow)
            return false;

        memoryCache.Set(dedupKey, true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheWindow
        });

        return true;
    }

    private static string BuildDedupKey(string clientIp, string path)
    {
        var normalizedPath = path.ToLowerInvariant();
        if (normalizedPath.Length > MaxRawPathKeyLength)
            normalizedPath = $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath)))}";

        return $"404_dedup:{clientIp}:{normalizedPath}";
    }

    private sealed class IpNotFoundCounter
    {
        public int Count;
    }
}
