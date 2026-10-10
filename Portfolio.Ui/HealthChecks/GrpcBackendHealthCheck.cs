using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Portfolio.Ui.HealthChecks;

public sealed class GrpcBackendHealthCheck(Health.HealthClient healthClient) : IHealthCheck
{
    public const string CheckName = "GrpcBackendCheck";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HealthCheckRequest { Service = "ready" };
            var response = await healthClient.CheckAsync(
                request,
                deadline: DateTime.UtcNow.AddSeconds(3),
                cancellationToken: cancellationToken);

            return response.Status switch
            {
                HealthCheckResponse.Types.ServingStatus.Serving => HealthCheckResult.Healthy("gRPC backend is healthy and serving."),
                HealthCheckResponse.Types.ServingStatus.NotServing => HealthCheckResult.Unhealthy("gRPC backend reported NOT_SERVING status."),
                _ => HealthCheckResult.Degraded($"gRPC backend reported unexpected status: {response.Status}")
            };
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to connect to gRPC backend service.", ex);
        }
    }
}
