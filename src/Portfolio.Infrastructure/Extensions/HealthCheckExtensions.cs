using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Portfolio.Infrastructure.Extensions;

public static class HealthCheckExtensions
{
    private const string LivenessCheckPostfix = "/liveness";
    private const string ReadinessCheckPostfix = "/readiness";
    private const string ThirdPartyCheckPostfix = "/thirdPartyHealthCheck";
    private const string ReadinessDbCheckName = "ReadinessDbCheck";
    
    public static bool IsThisPathHealthCheck(string path)
    {
        return path.StartsWith(LivenessCheckPostfix, StringComparison.OrdinalIgnoreCase)
               || path.StartsWith(ReadinessCheckPostfix, StringComparison.OrdinalIgnoreCase)
               || path.StartsWith(ThirdPartyCheckPostfix, StringComparison.OrdinalIgnoreCase);
    }
    
    extension(WebApplicationBuilder builder)
    {
        public IHealthChecksBuilder InitializeHealthChecks()
        {
            return builder.Services
                .AddHealthChecks();
        }

        public void AddHealthChecksForEndpointAndDb()
        {
            builder.InitializeHealthChecks()
                .AddCheck("LiveCheck", () => HealthCheckResult.Healthy("Live"), tags: ["live"])
                .AddDbContextCheck<PortfolioDbContext>(
                    failureStatus: HealthStatus.Degraded,
                    name: ReadinessDbCheckName,
                    tags: ["ready"]);
        }

        public void AddGrpcHealthChecksWithServices()
        {
            builder.Services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.Zero;
                options.Period = TimeSpan.FromSeconds(10);
            });

            builder.Services.AddGrpcHealthChecks(options =>
            {
                // Default service ("") checks general health
                options.Services.Map("", _ => true);
                // "ready" service checks dependencies (database)
                options.Services.Map("ready", r => r.Tags.Contains("ready"));
                // "live" service performs liveness check
                options.Services.Map("live", r => r.Tags.Contains("live"));
            });
        }
    }

    extension(WebApplication app)
    {
        public void MapLivenessHealthCheck()
        {
            app.MapHealthChecks(LivenessCheckPostfix, new HealthCheckOptions
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

        public void MapReadinessHealthCheck()
        {
            app.MapHealthChecks(ReadinessCheckPostfix, new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready") || check.Name == ReadinessDbCheckName,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status206PartialContent,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            });
        }

        public void MapHealthCheckForUptimeRobot()
        {
            app.MapMethods(ThirdPartyCheckPostfix, [HttpMethods.Head], () =>
            {
                return Results.Ok();
            });
        }
    }
}
