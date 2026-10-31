using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Portfolio.Grpc;
using Portfolio.Test.Shared;
using Portfolio.Ui.Services;
using Microsoft.Extensions.Logging.Abstractions;

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
        result.ActivePodCount.Should().Be(12);
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFromCacheAsync_WhenCacheMiss_CallsGrpcAndPopulatesCache()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var grpcResponse = new GetK8sStatsResponse
        {
            ActivePodCount = 15,
            ServiceCount = 8,
            NodeCount = 5,
            DeploymentCount = 5,
            CronJobCount = 1
        };

        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(grpcResponse));

        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act: First call (cache miss)
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().NotBeNull();
        result.ActivePodCount.Should().Be(15);
        result.ServiceCount.Should().Be(8);
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);

        // Act: Second call (cache hit)
        var cached = await service.GetFromCacheAsync();
        cached.Should().BeSameAs(result);
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshCacheAsync_AlwaysCallsGrpcAndUpdatesCache()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var firstResponse = new GetK8sStatsResponse { ActivePodCount = 10, NodeCount = 5 };
        var secondResponse = new GetK8sStatsResponse { ActivePodCount = 18, NodeCount = 5 };

        mockClient.SetupSequence(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(firstResponse))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(secondResponse));

        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act 1: Initial refresh
        var res1 = await service.RefreshCacheAsync();
        res1.ActivePodCount.Should().Be(10);

        // Act 2: Force refresh
        var res2 = await service.RefreshCacheAsync();
        res2.ActivePodCount.Should().Be(18);

        // Assert
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        memoryCache.Get<GetK8sStatsResponse>("k8s_stats")!.ActivePodCount.Should().Be(18);
    }

    [Fact]
    public async Task GetFromCacheAsync_PropagatesCancellationTokenToGrpcClient()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cts = new CancellationTokenSource();

        var grpcResponse = new GetK8sStatsResponse { ActivePodCount = 5 };
        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), cts.Token))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(grpcResponse));

        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act
        var result = await service.GetFromCacheAsync(cts.Token);

        // Assert
        result.ActivePodCount.Should().Be(5);
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task RefreshCacheAsync_PropagatesCancellationTokenToGrpcClient()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cts = new CancellationTokenSource();

        var grpcResponse = new GetK8sStatsResponse { ActivePodCount = 7 };
        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), cts.Token))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(grpcResponse));

        var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);

        // Act
        var result = await service.RefreshCacheAsync(cts.Token);

        // Assert
        result.ActivePodCount.Should().Be(7);
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetFromCacheAsync_ConcurrentRequestsOnCacheMiss_CallsGrpcOnlyOnce()
    {
        // Arrange
        var mockClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var grpcResponse = new GetK8sStatsResponse
        {
            ActivePodCount = 15,
            ServiceCount = 8,
            NodeCount = 5,
            DeploymentCount = 5,
            CronJobCount = 1
        };

        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                Thread.Sleep(50);
                return TestCallHelpers.CreateAsyncUnaryCall(grpcResponse);
            });

        // Act: 10 concurrent requests from different service instances sharing the same cache
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            var service = new ClusterStatsService(mockClient.Object, memoryCache, NullLogger<ClusterStatsService>.Instance);
            return await service.GetFromCacheAsync();
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert: All callers receive the stats, but gRPC was called only once (no stampede)
        results.Should().HaveCount(10);
        foreach (var result in results)
        {
            result.Should().NotBeNull();
            result.ActivePodCount.Should().Be(15);
        }

        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
