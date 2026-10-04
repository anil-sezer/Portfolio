using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Portfolio.Grpc;
using Portfolio.Test.Shared;
using Portfolio.Ui.Models;
using Portfolio.Ui.Services;

namespace Portfolio.Test.Ui;

[Collection("EnvironmentVariables")]
public class BackgroundImageServiceTests : IDisposable
{
    private readonly string? _origEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _origEnv);
    }

    [Fact]
    public async Task GetFromCacheAsync_InDevelopmentMode_ReturnsFallbackWithoutCallingGrpc()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new BackgroundImageService(mockClient.Object, memoryCache);

        // Act
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Contain("default-bg.jpg");
        result.Source.Should().Be(ImageOfTheDaySource.Bing);
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);

        // And it should have populated cache
        memoryCache.TryGetValue("background_image", out BackgroundImageModel? cached).Should().BeTrue();
        cached.Should().NotBeNull();
        cached!.Url.Should().Be(result.Url);
    }

    [Fact]
    public async Task GetFromCacheAsync_WhenCacheHit_ReturnsCachedValueWithoutCallingGrpc()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var preCached = new BackgroundImageModel
        {
            Url = "https://example.com/cached.jpg",
            AltText = "Cached Alt",
            Source = ImageOfTheDaySource.Nasa
        };
        memoryCache.Set("background_image", preCached);

        var service = new BackgroundImageService(mockClient.Object, memoryCache);

        // Act
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().BeSameAs(preCached);
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFromCacheAsync_WhenCacheMiss_CallsGrpcAndCachesResult()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var grpcResponse = new BackgroundImageDetails
        {
            Url = "https://example.com/prod-bg.jpg",
            AltText = "Production Alt",
            Source = ImageOfTheDaySource.Nasa
        };

        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(grpcResponse));

        var service = new BackgroundImageService(mockClient.Object, memoryCache);

        // Act: First call (cache miss)
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Be("https://example.com/prod-bg.jpg");
        result.AltText.Should().Be("Production Alt");
        result.Source.Should().Be(ImageOfTheDaySource.Nasa);

        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);

        // Act: Second call (cache hit)
        var cachedResult = await service.GetFromCacheAsync();
        cachedResult.Url.Should().Be(result.Url);

        // Should still only have called gRPC once!
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFromCacheAsync_WhenGrpcReturnsNoneSource_ReturnsFallbackDefaultBg()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var grpcResponse = new BackgroundImageDetails
        {
            Url = "",
            AltText = "",
            Source = ImageOfTheDaySource.None
        };

        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(grpcResponse));

        var service = new BackgroundImageService(mockClient.Object, memoryCache);

        // Act
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Contain("default-bg.jpg");
        result.Source.Should().Be(ImageOfTheDaySource.Bing);
    }

    [Fact]
    public async Task GetFromCacheAsync_PropagatesCancellationTokenToGrpcClient()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cts = new CancellationTokenSource();

        var grpcResponse = new BackgroundImageDetails
        {
            Url = "https://example.com/test.jpg",
            AltText = "Test",
            Source = ImageOfTheDaySource.Bing
        };

        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), cts.Token))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(grpcResponse));

        var service = new BackgroundImageService(mockClient.Object, memoryCache);

        // Act
        var result = await service.GetFromCacheAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetFromCacheAsync_WhenGrpcThrowsRpcException_PropagatesException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "gRPC service down")));

        var service = new BackgroundImageService(mockClient.Object, memoryCache);

        // Act
        var act = async () => await service.GetFromCacheAsync();

        // Assert
        await act.Should().ThrowAsync<RpcException>()
            .WithMessage("*gRPC service down*");
    }
}
