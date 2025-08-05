using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;

namespace Portfolio.Ui.Services;

public class ClusterStatsService(K8sStats.K8sStatsClient k8SStatsClient, IMemoryCache memoryCache)
{
    private const string CacheKey = "k8s_stats";
    
    public async Task<GetK8sStatsResponse> GetFromCacheAsync()
    {
        if (memoryCache.TryGetValue(CacheKey, out GetK8sStatsResponse? cachedStats) && cachedStats != null)
        {
            Log.Information("📦 K8s stats retrieved from cache");
            return cachedStats;
        }

        var stats = await GetFromGrpcAsync();
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        
        memoryCache.Set(CacheKey, stats, cacheOptions);
        Log.Information("💾 K8s stats added to cache with 5-min expiration");
        
        return stats;
    }
    
    private async Task<GetK8sStatsResponse> GetFromGrpcAsync()
    {
        var response = await k8SStatsClient.GetAsync(new Empty());
        Log.Information("\ud83d\udce8 Sent a gRPC request to {ServiceName}", nameof(k8SStatsClient.GetAsync));

        return response;
    }
}
