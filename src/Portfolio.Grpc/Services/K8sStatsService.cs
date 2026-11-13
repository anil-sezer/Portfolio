using k8s;
using Microsoft.Extensions.Caching.Memory;

namespace Portfolio.Grpc.Services;

// ReSharper disable once InconsistentNaming
public class K8sStatsService(
    IKubernetes client,
    IMemoryCache cache,
    ILogger<K8sStatsService> logger) : K8sStats.K8sStatsBase
{
    private const string CacheKey = "grpc_k8s_stats";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public override async Task<GetK8sStatsResponse> Get(Empty request, ServerCallContext context)
    {
        if (cache.TryGetValue(CacheKey, out GetK8sStatsResponse? cachedStats) && cachedStats != null)
        {
            logger.LogDebug("Returning cached cluster statistics");
            return cachedStats;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            cts.CancelAfter(DefaultTimeout);
            var cancellationToken = cts.Token;

            var pods = await client.CoreV1.ListPodForAllNamespacesAsync(cancellationToken: cancellationToken);
            var services = await client.CoreV1.ListServiceForAllNamespacesAsync(cancellationToken: cancellationToken);
            var nodes = await client.CoreV1.ListNodeAsync(cancellationToken: cancellationToken);
            var deployments = await client.AppsV1.ListDeploymentForAllNamespacesAsync(cancellationToken: cancellationToken);
            var cronJobs = await client.BatchV1.ListCronJobForAllNamespacesAsync(cancellationToken: cancellationToken);

            var response = new GetK8sStatsResponse
            {
                ActivePodCount = pods.Items.Count(pod => pod.Status.Phase == "Running"),
                ServiceCount = services.Items.Count,
                NodeCount = nodes.Items.Count + 1, // +1 for the NAS. It's not managed by Kubernetes but is an integral part of the cluster
                DeploymentCount = deployments.Items.Count,
                CronJobCount = cronJobs.Items.Count(cronJob => !cronJob.Spec.Suspend.GetValueOrDefault(false))
            };

            cache.Set(CacheKey, response, CacheDuration);
            return response;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve cluster statistics", ex);
        }
    }
}