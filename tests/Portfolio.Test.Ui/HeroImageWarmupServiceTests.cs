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
        var mockService = CreateMockService();
        mockService.Setup(s => s.RefreshCacheAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => mockService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var warmupService = new HeroImageWarmupService(serviceProvider, NullLogger<HeroImageWarmupService>.Instance);
        using var cts = new CancellationTokenSource();

        // Cancel after short delay so ExecuteAsync's periodic timer loop exits
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        // Act
        await warmupService.StartAsync(cts.Token);
        await Task.Delay(50);
        await warmupService.StopAsync(CancellationToken.None);

        // Assert
        mockService.Verify(s => s.RefreshCacheAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshThrows_DoesNotCrashService()
    {
        // Arrange
        var mockService = CreateMockService();
        mockService.Setup(s => s.RefreshCacheAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated error"));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => mockService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var warmupService = new HeroImageWarmupService(serviceProvider, NullLogger<HeroImageWarmupService>.Instance);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        // Act
        var act = async () =>
        {
            await warmupService.StartAsync(cts.Token);
            await Task.Delay(50);
            await warmupService.StopAsync(CancellationToken.None);
        };

        // Assert
        await act.Should().NotThrowAsync();
        mockService.Verify(s => s.RefreshCacheAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
