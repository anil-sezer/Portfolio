using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portfolio.Grpc;
using Portfolio.Ui.BackgroundServices;
using Portfolio.Ui.Services;

namespace Portfolio.Test.Ui;

public class HeroImageWarmupServiceTests
{
    private static Mock<BackgroundImageService> CreateMockService()
    {
        var mockGrpcClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        return new Mock<BackgroundImageService>(
            mockGrpcClient.Object,
            memoryCache,
            NullLogger<BackgroundImageService>.Instance,
            new HttpClient());
    }

    [Fact]
    public async Task ExecuteAsync_OnStartup_InvokesRefreshCacheAsync()
    {
        // Arrange
        var refreshCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mockService = CreateMockService();
        mockService.Setup(s => s.RefreshCacheAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                refreshCalled.TrySetResult(true);
                return Task.CompletedTask;
            });

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => mockService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var warmupService = new HeroImageWarmupService(serviceProvider, NullLogger<HeroImageWarmupService>.Instance);

        // Act
        await warmupService.StartAsync(CancellationToken.None);
        var completed = await Task.WhenAny(refreshCalled.Task, Task.Delay(5000));
        await warmupService.StopAsync(CancellationToken.None);

        // Assert
        completed.Should().Be(refreshCalled.Task);
        mockService.Verify(s => s.RefreshCacheAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
                return Task.FromException(new InvalidOperationException("Simulated error"));
            });

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => mockService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var warmupService = new HeroImageWarmupService(serviceProvider, NullLogger<HeroImageWarmupService>.Instance);

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
}
