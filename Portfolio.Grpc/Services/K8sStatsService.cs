using k8s;

namespace Portfolio.Grpc.Services;

// ReSharper disable once InconsistentNaming
public class K8sStatsService: K8sStats.K8sStatsBase
{
    private readonly IKubernetes _client;

    public K8sStatsService()
    {
        KubernetesClientConfiguration config;
    
        if (KubernetesClientConfiguration.IsInCluster())
            config = KubernetesClientConfiguration.InClusterConfig();
        else
            config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
    
        _client = new Kubernetes(config);
    }

    public override async Task<GetK8sStatsResponse> Get(Empty request, ServerCallContext context)
    {
        try
        {
            // Get all resources in parallel because why not, let's try if it speeds up the process or does something odd.
            var podsTask = _client.CoreV1.ListPodForAllNamespacesAsync();
            var servicesTask = _client.CoreV1.ListServiceForAllNamespacesAsync();
            var nodesTask = _client.CoreV1.ListNodeAsync();
            var deploymentsTask = _client.AppsV1.ListDeploymentForAllNamespacesAsync();
            var cronJobsTask = _client.BatchV1.ListCronJobForAllNamespacesAsync();

            await Task.WhenAll(podsTask, servicesTask, nodesTask, deploymentsTask);

            await Task.WhenAll(podsTask, servicesTask, nodesTask, deploymentsTask, cronJobsTask);

            var pods = await podsTask;
            var services = await servicesTask;
            var nodes = await nodesTask;
            var deployments = await deploymentsTask;
            var cronJobs = await cronJobsTask;
            
            return new GetK8sStatsResponse
            {
                ActivePodCount = pods.Items.Count(pod => pod.Status.Phase == "Running"),
                ServiceCount = services.Items.Count,
                NodeCount = nodes.Items.Count + 1, // +1 for the NAS. It's not managed by Kubernetes but is an integral part of the cluster
                DeploymentCount = deployments.Items.Count,
                CronJobCount = cronJobs.Items.Count(cronJob => !cronJob.Spec.Suspend.GetValueOrDefault(false))

            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve cluster statistics", ex);
        }
    }
}