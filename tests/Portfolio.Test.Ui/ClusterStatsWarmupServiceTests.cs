using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portfolio.Grpc;
using Portfolio.Ui.BackgroundServices;
using Portfolio.Ui.Services;

namespace Portfolio.Test.Ui;

public class ClusterStatsWarmupServiceTests
{
    private static Mock<ClusterStatsService> CreateMockService()
    {
        var mockGrpcClient = new Mock<K8sStats.K8sStatsClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        return new Mock<ClusterStatsService>(
            mockGrpcClient.Object,
            memoryCache,
            NullLogger<ClusterStatsService>.Instance);
    }

    private static ServiceProvider CreateServiceProvider(ClusterStatsService service)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => service);
        return serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public void DefaultConfiguration_UsesThirtySecondIntervalWithJitter()
    {
        // Arrange
        var mockService = CreateMockService();
        using var serviceProvider = CreateServiceProvider(mockService.Object);
        var warmupService = new ClusterStatsWarmupService(serviceProvider, NullLogger<ClusterStatsWarmupService>.Instance);

        // Act
        var refreshDelays = Enumerable.Range(0, 20)
            .Select(_ => warmupService.GetNextRefreshDelay())
            .ToList();
        var startupDelays = Enumerable.Range(0, 20)
            .Select(_ => warmupService.GetStartupDelay())
            .ToList();

        // Assert
        ClusterStatsWarmupService.DefaultRefreshInterval.Should().Be(TimeSpan.FromSeconds(30));
        refreshDelays.Should().OnlyContain(d =>
            d >= ClusterStatsWarmupService.DefaultRefreshInterval &&
            d <= ClusterStatsWarmupService.DefaultRefreshInterval + ClusterStatsWarmupService.DefaultMaxJitter);
        refreshDelays.Distinct().Count().Should().BeGreaterThan(1);

        startupDelays.Should().OnlyContain(d =>
            d >= TimeSpan.Zero &&
            d <= ClusterStatsWarmupService.DefaultMaxStartupJitter);
    }

    [Theory]
    [InlineData(0.0, 30_000)]
    [InlineData(0.5, 35_000)]
    [InlineData(1.0, 40_000)]
    public void GetNextRefreshDelay_AppliesRandomJitterWithinBounds(double randomValue, double expectedMilliseconds)
    {
        // Arrange
        var mockService = CreateMockService();
        using var serviceProvider = CreateServiceProvider(mockService.Object);
        var warmupService = new ClusterStatsWarmupService(
            serviceProvider,
            NullLogger<ClusterStatsWarmupService>.Instance,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(5),
            () => randomValue);

        // Act
        var delay = warmupService.GetNextRefreshDelay();

        // Assert
        delay.TotalMilliseconds.Should().BeApproximately(expectedMilliseconds, 0.001);
    }

    [Fact]
    public async Task ExecuteAsync_OnStartupAndPeriodicTick_InvokesRefreshCacheAsync()
    {
        // Arrange
        var refreshCount = 0;
        var secondRefreshCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mockService = CreateMockService();
        mockService.Setup(s => s.RefreshCacheAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (Interlocked.Increment(ref refreshCount) >= 2)
                {
                    secondRefreshCalled.TrySetResult(true);
                }

                return Task.FromResult(new GetK8sStatsResponse());
            });

        using var serviceProvider = CreateServiceProvider(mockService.Object);
        var warmupService = new ClusterStatsWarmupService(
            serviceProvider,
            NullLogger<ClusterStatsWarmupService>.Instance,
            baseRefreshInterval: TimeSpan.FromMilliseconds(20),
            maxJitter: TimeSpan.FromMilliseconds(10),
            maxStartupJitter: TimeSpan.Zero);

        // Act
        await warmupService.StartAsync(CancellationToken.None);
        var completed = await Task.WhenAny(secondRefreshCalled.Task, Task.Delay(5000));
        await warmupService.StopAsync(CancellationToken.None);

        // Assert
        completed.Should().Be(secondRefreshCalled.Task);
        refreshCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshThrows_DoesNotCrashService()
    {
        // Arrange
        var refreshCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mockService = CreateMockService();
        mockService.Setup(s => s.RefreshCacheAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                refreshCalled.TrySetResult(true);
                return Task.FromException<GetK8sStatsResponse>(new InvalidOperationException("Simulated gRPC error"));
            });

        using var serviceProvider = CreateServiceProvider(mockService.Object);
        var warmupService = new ClusterStatsWarmupService(
            serviceProvider,
            NullLogger<ClusterStatsWarmupService>.Instance,
            baseRefreshInterval: TimeSpan.FromMilliseconds(50),
            maxJitter: TimeSpan.Zero,
            maxStartupJitter: TimeSpan.Zero);

        // Act
        var act = async () =>
        {
            await warmupService.StartAsync(CancellationToken.None);
            var completed = await Task.WhenAny(refreshCalled.Task, Task.Delay(5000));
            completed.Should().Be(refreshCalled.Task);
            await warmupService.StopAsync(CancellationToken.None);
        };

        // Assert
        await act.Should().NotThrowAsync();
        mockService.Verify(s => s.RefreshCacheAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoppedDuringStartupDelay_ExitsCleanlyWithoutCallingRefresh()
    {
        // Arrange
        var mockService = CreateMockService();
        using var serviceProvider = CreateServiceProvider(mockService.Object);
        var warmupService = new ClusterStatsWarmupService(
            serviceProvider,
            NullLogger<ClusterStatsWarmupService>.Instance,
            baseRefreshInterval: TimeSpan.FromMinutes(5),
            maxJitter: TimeSpan.FromMinutes(1),
            maxStartupJitter: TimeSpan.FromSeconds(30),
            () => 1.0);

        // Act
        await warmupService.StartAsync(CancellationToken.None);
        var act = () => warmupService.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        mockService.Verify(s => s.RefreshCacheAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
