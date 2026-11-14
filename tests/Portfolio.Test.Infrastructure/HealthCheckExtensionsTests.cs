using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Portfolio.Infrastructure.Extensions;

namespace Portfolio.Test.Infrastructure;

public class HealthCheckExtensionsTests
{
    [Theory]
    [InlineData("/liveness")]
    [InlineData("/liveness/subpath")]
    [InlineData("/LIVENESS")]
    [InlineData("/readiness")]
    [InlineData("/readiness/detail")]
    [InlineData("/READINESS")]
    [InlineData("/thirdPartyHealthCheck")]
    [InlineData("/THIRDPARTYHEALTHCHECK")]
    [InlineData("/grpc.health.v1.Health/Check")]
    [InlineData("/grpc.health.v1.Health/Watch")]
    [InlineData("/GRPC.HEALTH.V1.HEALTH/CHECK")]
    public void IsThisPathHealthCheck_WhenHealthCheckPath_ReturnsTrue(string path)
    {
        // Act
        var result = HealthCheckExtensions.IsThisPathHealthCheck(path);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/tr")]
    [InlineData("/api/cluster/stats")]
    [InlineData("/client_info.K8sStats/Get")]
    [InlineData("/visitor_insights.VisitorInsights/StoreVisitorInfo")]
    [InlineData("/api/visits/log")]
    [InlineData("/api/email/send")]
    [InlineData("/Error")]
    [InlineData("/health")]
    public void IsThisPathHealthCheck_WhenNotHealthCheckPath_ReturnsFalse(string path)
    {
        // Act
        var result = HealthCheckExtensions.IsThisPathHealthCheck(path);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AddHealthChecksForEndpointAndDb_RegistersReadinessDbCheckWithUnhealthyFailureStatus()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddHealthChecksForEndpointAndDb();
        using var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var dbRegistration = options.Registrations.Should()
            .ContainSingle(r => r.Name == "ReadinessDbCheck")
            .Subject;

        dbRegistration.FailureStatus.Should().Be(HealthStatus.Unhealthy);
        dbRegistration.Tags.Should().Contain("ready");
    }

    [Theory]
    [InlineData(HealthStatus.Healthy, StatusCodes.Status200OK)]
    [InlineData(HealthStatus.Unhealthy, StatusCodes.Status503ServiceUnavailable)]
    public async Task MapReadinessHealthCheck_MapsHealthStatusToExpectedHttpStatusCode(
        HealthStatus checkStatus,
        int expectedStatusCode)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.InitializeHealthChecks()
            .AddCheck("TestReadyCheck", () => new HealthCheckResult(checkStatus), tags: ["ready"]);

        await using var app = builder.Build();
        app.MapReadinessHealthCheck();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Should()
            .ContainSingle(e => e.RoutePattern.RawText == "/readiness")
            .Subject;

        var httpContext = new DefaultHttpContext
        {
            RequestServices = app.Services
        };

        // Act
        await endpoint.RequestDelegate!(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(expectedStatusCode);
    }
}
