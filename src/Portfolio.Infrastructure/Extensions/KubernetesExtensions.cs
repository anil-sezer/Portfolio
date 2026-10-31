using k8s;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Portfolio.Infrastructure.Extensions;

public static class KubernetesExtensions
{
    public static void SetupKubernetesClient(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IKubernetes>(_ =>
        {
            var config = KubernetesClientConfiguration.IsInCluster()
                ? KubernetesClientConfiguration.InClusterConfig()
                : KubernetesClientConfiguration.BuildConfigFromConfigFile();

            return new Kubernetes(config);
        });
    }
}
