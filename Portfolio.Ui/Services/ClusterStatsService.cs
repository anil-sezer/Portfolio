using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;

namespace Portfolio.Ui.Services;

public class ClusterStatsService(K8sStats.K8sStatsClient k8SStatsClient, IMemoryCache memoryCache)
{
    private const string CacheKey = "k8s_stats";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public async Task<GetK8sStatsResponse> GetFromCacheAsync(CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(CacheKey, out GetK8sStatsResponse? cachedStats) && cachedStats != null)
            return cachedStats;

        // Fallback on initial call or rare cache miss before first background refresh finishes
        return await RefreshCacheAsync(cancellationToken);
    }

    public async Task<GetK8sStatsResponse> RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        var stats = await GetFromGrpcAsync(cancellationToken);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration
        };

        memoryCache.Set(CacheKey, stats, cacheOptions);
        Log.Information("💾 K8s stats updated in cache");

        return stats;
    }

    private async Task<GetK8sStatsResponse> GetFromGrpcAsync(CancellationToken cancellationToken = default)
    {
        var response = await k8SStatsClient.GetAsync(new Empty(), cancellationToken: cancellationToken);
        Log.Information("📨 Sent a gRPC request to {ServiceName}", nameof(k8SStatsClient.GetAsync));

        return response;
    }
}
