using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portfolio.Grpc;
using Portfolio.Ui.Endpoints;
using Portfolio.Ui.Models;
using Portfolio.Ui.Services;

namespace Portfolio.Test.Ui;

public class ClusterStatsEndpointsTests
{
    [Fact]
    public async Task GetStats_WhenCacheContainsStats_ReturnsOkWithClusterStatsResponse()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var preCached = new GetK8sStatsResponse
        {
            ActivePodCount = 18,
            ServiceCount = 12,
            NodeCount = 5,
            DeploymentCount = 10,
            CronJobCount = 3
        };
        memoryCache.Set("k8s_stats", preCached);

        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act
        var result = await ClusterStatsEndpoints.GetStats(service, NullLoggerFactory.Instance, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ClusterStatsResponse>>();
        var okResult = (Ok<ClusterStatsResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.ActivePodCount.Should().Be(18);
        okResult.Value!.ServiceCount.Should().Be(12);
        okResult.Value!.NodeCount.Should().Be(5);
        okResult.Value!.DeploymentCount.Should().Be(10);
        okResult.Value!.CronJobCount.Should().Be(3);
    }

    [Fact]
    public async Task GetStats_WhenOperationCanceled_Returns499ClientClosedRequest()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var result = await ClusterStatsEndpoints.GetStats(service, NullLoggerFactory.Instance, cts.Token);

        // Assert
        result.Should().BeOfType<StatusCodeHttpResult>();
        ((StatusCodeHttpResult)result).StatusCode.Should().Be(499);
    }

    [Fact]
    public async Task GetStats_WhenServiceThrowsException_Returns503ServiceUnavailable()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        mockClient
            .Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "K8s service unreachable")));

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act
        var result = await ClusterStatsEndpoints.GetStats(service, NullLoggerFactory.Instance, CancellationToken.None);

        // Assert
        result.Should().BeOfType<StatusCodeHttpResult>();
        ((StatusCodeHttpResult)result).StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }
}
