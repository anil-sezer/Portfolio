using FluentAssertions;
using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Portfolio.Test.Shared;
using Portfolio.Ui.HealthChecks;

namespace Portfolio.Test.Ui.HealthChecks;

public class GrpcBackendHealthCheckTests
{
    private readonly Mock<Health.HealthClient> _mockHealthClient;
    private readonly GrpcBackendHealthCheck _healthCheck;

    public GrpcBackendHealthCheckTests()
    {
        _mockHealthClient = new Mock<Health.HealthClient>();
        _healthCheck = new GrpcBackendHealthCheck(_mockHealthClient.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenServing()
    {
        // Arrange
        var response = new HealthCheckResponse { Status = HealthCheckResponse.Types.ServingStatus.Serving };
        var asyncCall = TestCallHelpers.CreateAsyncUnaryCall(response);

        _mockHealthClient
            .Setup(c => c.CheckAsync(
                It.Is<HealthCheckRequest>(r => r.Service == "ready"),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(asyncCall);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("healthy and serving");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenNotServing()
    {
        // Arrange
        var response = new HealthCheckResponse { Status = HealthCheckResponse.Types.ServingStatus.NotServing };
        var asyncCall = TestCallHelpers.CreateAsyncUnaryCall(response);

        _mockHealthClient
            .Setup(c => c.CheckAsync(
                It.Is<HealthCheckRequest>(r => r.Service == "ready"),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(asyncCall);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("NOT_SERVING");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenRpcThrows()
    {
        // Arrange
        _mockHealthClient
            .Setup(c => c.CheckAsync(
                It.IsAny<HealthCheckRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "Connection refused")));

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().NotBeNull();
        result.Description.Should().Contain("Failed to connect");
    }
}
