using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Portfolio.Infrastructure.Constants;

namespace Portfolio.Infrastructure.Extensions;

public static class HealthCheckExtensions
{
    private const string ReadinessDbCheckName = "ReadinessDbCheck";
    
    public static IHealthChecksBuilder InitializeHealthChecks(this WebApplicationBuilder builder)
    {
        return builder.Services
            .AddHealthChecks();
    }
    
    public static void AddHealthChecksForEndpointAndDb(this WebApplicationBuilder builder)
    {
        builder.InitializeHealthChecks()
            .AddDbContextCheck<PortfolioDbContext>(failureStatus: HealthStatus.Degraded, name: ReadinessDbCheckName);
    }
    
    public static void MapLivenessHealthCheck(this WebApplication app)
    {
        app.MapHealthChecks("/liveness", new HealthCheckOptions
        {
            Predicate = _ => false, // Always return healthy for liveness
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK, // Liveness doesn't degrade
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });
    }
    
    // todo: Maybe do a grpc endpoint check? Or maybe read this healthcheck stuff again. Seems like I'm missing something. 
    public static void MapReadinessHealthCheck(this WebApplication app)
    {
        app.MapHealthChecks("/readiness", new HealthCheckOptions
        {
            Predicate = check => check.Name == ReadinessDbCheckName,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status206PartialContent,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });
    }
    
    public static void MapHealthCheckForUptimeRobot(this WebApplication app)
    {
        app.MapMethods("/thirdPartyHealthCheck", [HttpMethods.Head], () =>
        {
            return Results.Ok();
        });
    }
}
