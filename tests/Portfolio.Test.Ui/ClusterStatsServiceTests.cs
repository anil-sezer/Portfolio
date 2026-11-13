using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portfolio.Grpc;
using Portfolio.Test.Shared;
using Portfolio.Ui.Services;

namespace Portfolio.Test.Ui;

public class ClusterStatsServiceTests
{
    [Fact]
    public async Task GetFromCacheAsync_WhenCacheHit_ReturnsCachedStatsWithoutCallingGrpc()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var preCached = new GetK8sStatsResponse
        {
            ActivePodCount = 12,
            ServiceCount = 6,
            NodeCount = 5,
            DeploymentCount = 4,
            CronJobCount = 2
        };
        memoryCache.Set("k8s_stats", preCached);

        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().BeSameAs(preCached);
        result!.ActivePodCount.Should().Be(12);
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GetCachedStats_WhenCacheHit_ReturnsCachedStatsSynchronouslyWithoutCallingGrpc()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var preCached = new GetK8sStatsResponse
        {
            ActivePodCount = 20,
            ServiceCount = 10,
            NodeCount = 5,
            DeploymentCount = 6,
            CronJobCount = 3
        };
        memoryCache.Set("k8s_stats", preCached);

        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act
        var result = service.GetCachedStats();

        // Assert
        result.Should().BeSameAs(preCached);
        result!.ActivePodCount.Should().Be(20);
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFromCacheAsync_WhenCacheMiss_ReturnsNullWithoutCallingGrpc()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act: Cache miss should never block or trigger live gRPC call
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().BeNull();
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GetCachedStats_WhenCacheMiss_ReturnsNullSynchronouslyWithoutCallingGrpc()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act
        var result = service.GetCachedStats();

        // Assert
        result.Should().BeNull();
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshCacheAsync_AlwaysCallsGrpcWithDeadlineToUpdateCache()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var firstResponse = new GetK8sStatsResponse { ActivePodCount = 10, NodeCount = 5 };
        var secondResponse = new GetK8sStatsResponse { ActivePodCount = 18, NodeCount = 5 };

        mockClient.SetupSequence(c => c.GetAsync(
                It.IsAny<Empty>(),
                It.IsAny<Metadata>(),
                It.Is<DateTime?>(d => d.HasValue && d.Value > DateTime.UtcNow),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(firstResponse))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(secondResponse));

        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act 1: Initial background refresh
        var res1 = await service.RefreshCacheAsync();
        res1.ActivePodCount.Should().Be(10);

        // Act 2: Periodic background refresh
        var res2 = await service.RefreshCacheAsync();
        res2.ActivePodCount.Should().Be(18);

        // Assert
        mockClient.Verify(c => c.GetAsync(
            It.IsAny<Empty>(),
            It.IsAny<Metadata>(),
            It.Is<DateTime?>(d => d.HasValue && d.Value > DateTime.UtcNow),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        memoryCache.Get<GetK8sStatsResponse>("k8s_stats")!.ActivePodCount.Should().Be(18);
    }

    [Fact]
    public async Task GetFromCacheAsync_ConcurrentRequestsOnCacheMiss_NeverCallsGrpc()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Act: Multiple concurrent page-load requests when cache is not yet warmed
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);
            return await service.GetFromCacheAsync();
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert: All callers return null instantly without queuing behind a lock or calling gRPC
        results.Should().HaveCount(10);
        foreach (var result in results)
        {
            result.Should().BeNull();
        }

        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
