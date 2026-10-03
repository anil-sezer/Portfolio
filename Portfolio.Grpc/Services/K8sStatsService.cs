using k8s;

namespace Portfolio.Grpc.Services;

// ReSharper disable once InconsistentNaming
public class K8sStatsService(IKubernetes client) : K8sStats.K8sStatsBase
{

    public override async Task<GetK8sStatsResponse> Get(Empty request, ServerCallContext context)
    {
        try
        {
            // Get all resources in parallel because why not, let's try if it speeds up the process or does something odd.
            var podsTask = client.CoreV1.ListPodForAllNamespacesAsync(cancellationToken: context.CancellationToken);
            var servicesTask = client.CoreV1.ListServiceForAllNamespacesAsync(cancellationToken: context.CancellationToken);
            var nodesTask = client.CoreV1.ListNodeAsync(cancellationToken: context.CancellationToken);
            var deploymentsTask = client.AppsV1.ListDeploymentForAllNamespacesAsync(cancellationToken: context.CancellationToken);
            var cronJobsTask = client.BatchV1.ListCronJobForAllNamespacesAsync(cancellationToken: context.CancellationToken);

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