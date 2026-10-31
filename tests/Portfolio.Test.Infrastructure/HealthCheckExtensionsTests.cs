using FluentAssertions;
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
}
