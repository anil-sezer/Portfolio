using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;

namespace Portfolio.Ui.Services;

public class ClusterStatsService(
    K8sStats.K8sStatsClient k8SStatsClient,
    IMemoryCache memoryCache,
    ILogger<ClusterStatsService> logger)
{
    private const string CacheKey = "k8s_stats";
    private static readonly TimeSpan GrpcTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Synchronously retrieves the last known / warm-cached cluster stats from memory.
    /// Never calls gRPC or blocks on network operations.
    /// </summary>
    public virtual GetK8sStatsResponse? GetCachedStats()
    {
        if (memoryCache.TryGetValue(CacheKey, out GetK8sStatsResponse? cachedStats) && cachedStats != null)
        {
            return cachedStats;
        }

        return null;
    }

    /// <summary>
    /// Asynchronously retrieves the last known / warm-cached cluster stats from memory.
    /// Never calls gRPC or blocks on network operations.
    /// </summary>
    public virtual Task<GetK8sStatsResponse?> GetFromCacheAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetCachedStats());
    }

    /// <summary>
    /// Fetches the latest cluster stats from gRPC and updates the in-memory cache.
    /// Called periodically in the background by <see cref="BackgroundServices.ClusterStatsWarmupService"/>.
    /// </summary>
    public virtual async Task<GetK8sStatsResponse> RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        var stats = await GetFromGrpcAsync(cancellationToken);

        memoryCache.Set(CacheKey, stats);
        logger.LogInformation("💾 K8s stats updated in cache");

        return stats;
    }

    private async Task<GetK8sStatsResponse> GetFromGrpcAsync(CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.Add(GrpcTimeout);
        var response = await k8SStatsClient.GetAsync(new Empty(), deadline: deadline, cancellationToken: cancellationToken);
        logger.LogInformation("📨 Sent a gRPC request to {ServiceName}", nameof(k8SStatsClient.GetAsync));

        return response;
    }
}
