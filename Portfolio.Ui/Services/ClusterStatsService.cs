using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;

namespace Portfolio.Ui.Services;

public class ClusterStatsService(
    K8sStats.K8sStatsClient k8SStatsClient,
    IMemoryCache memoryCache,
    ILogger<ClusterStatsService> logger)
{
    private const string CacheKey = "k8s_stats";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    public virtual async Task<GetK8sStatsResponse> GetFromCacheAsync(CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(CacheKey, out GetK8sStatsResponse? cachedStats) && cachedStats != null)
            return cachedStats;

        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            if (memoryCache.TryGetValue(CacheKey, out cachedStats) && cachedStats != null)
                return cachedStats;

            return await memoryCache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheExpiration;
                var stats = await GetFromGrpcAsync(cancellationToken);
                logger.LogInformation("💾 K8s stats updated in cache");
                return stats;
            }) ?? throw new InvalidOperationException("Failed to retrieve K8s stats from cache.");
        }
        finally
        {
            CacheLock.Release();
        }
    }

    public virtual async Task<GetK8sStatsResponse> RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        var stats = await GetFromGrpcAsync(cancellationToken);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration
        };

        memoryCache.Set(CacheKey, stats, cacheOptions);
        logger.LogInformation("💾 K8s stats updated in cache");

        return stats;
    }

    private async Task<GetK8sStatsResponse> GetFromGrpcAsync(CancellationToken cancellationToken = default)
    {
        var response = await k8SStatsClient.GetAsync(new Empty(), cancellationToken: cancellationToken);
        logger.LogInformation("📨 Sent a gRPC request to {ServiceName}", nameof(k8SStatsClient.GetAsync));

        return response;
    }
}
