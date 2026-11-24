using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portfolio.Grpc;
using Portfolio.Infrastructure.Constants;
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
        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

        // Act
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Contain("default-bg.webp");
        result.DesktopImageUrl.Should().Be("img/default-bg.webp");
        result.MobileImageUrl.Should().Be("img/default-bg-mobile.webp");
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

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

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

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

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

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

        // Act
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Contain("default-bg.webp");
        result.Source.Should().Be(ImageOfTheDaySource.Bing);
    }

    [Fact]
    public async Task GetHeroImageBytesAsync_WhenRemoteUrl_FetchesFromGrpcAndCaches()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var preCached = new BackgroundImageModel
        {
            Url = "https://www.bing.com/th?id=OHR.Test_1920x1080.jpg",
            AltText = "Bing Alt",
            Source = ImageOfTheDaySource.Bing
        };
        memoryCache.Set("background_image", preCached);

        var webpBytes = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x01, 0x02 };
        mockClient.Setup(c => c.GetImageBytesAsync(
                It.Is<HeroImageRequest>(r => r.Variant == "mobile"),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(new HeroImageResponse
            {
                Data = ByteString.CopyFrom(webpBytes),
                ContentType = "image/webp",
                Version = preCached.Version,
                IsStale = false
            }));

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

        // Act
        var (mobileBytes, isStale) = await service.GetHeroImageBytesAsync(DeviceTypes.Mobile);
        var (cachedMobileBytes, isCachedStale) = await service.GetHeroImageBytesAsync(DeviceTypes.Mobile);

        // Assert
        mobileBytes.Should().Equal(webpBytes);
        isStale.Should().BeFalse();
        cachedMobileBytes.Should().BeSameAs(mobileBytes);
        isCachedStale.Should().BeFalse();
        preCached.MobileImageUrl.Should().StartWith("/api/hero-image/mobile?v=");
        preCached.DesktopImageUrl.Should().StartWith("/api/hero-image/desktop?v=");

        mockClient.Verify(c => c.GetImageBytesAsync(
            It.Is<HeroImageRequest>(r => r.Variant == "mobile"),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFromCacheAsync_WhenGrpcThrowsRpcException_ReturnsFallbackImage()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "gRPC service down")));

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

        // Act
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsFallback.Should().BeTrue();
        result.DesktopImageUrl.Should().Be("img/default-bg.webp");
        result.MobileImageUrl.Should().Be("img/default-bg-mobile.webp");
        result.Source.Should().Be(ImageOfTheDaySource.Bing);
        memoryCache.TryGetValue("background_image", out BackgroundImageModel? cached).Should().BeTrue();
        cached.Should().NotBeNull();
        cached!.IsFallback.Should().BeTrue();
    }

    [Fact]
    public async Task GetFromCacheAsync_WhenCancelledAndGrpcThrowsRpcException_PropagatesException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cts = new CancellationTokenSource();

        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .Throws(new RpcException(new Status(StatusCode.Cancelled, "Request cancelled")));

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

        // Act
        var act = async () => await service.GetFromCacheAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<RpcException>();
        memoryCache.TryGetValue("background_image", out _).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshCacheAsync_ProactivelyCachesMetadataAndVariants()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var grpcResponse = new BackgroundImageDetails
        {
            Url = "https://www.bing.com/th?id=OHR.Proactive_1920x1080.jpg",
            AltText = "Proactive Alt",
            Source = ImageOfTheDaySource.Bing
        };

        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(grpcResponse));

        var desktopPayload = new byte[] { 10, 20, 30 };
        var mobilePayload = new byte[] { 40, 50, 60 };

        mockClient.Setup(c => c.GetImageBytesAsync(
                It.Is<HeroImageRequest>(r => r.Variant == "desktop"),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(new HeroImageResponse
            {
                Data = ByteString.CopyFrom(desktopPayload),
                ContentType = "image/webp",
                IsStale = false
            }));

        mockClient.Setup(c => c.GetImageBytesAsync(
                It.Is<HeroImageRequest>(r => r.Variant == "mobile"),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(new HeroImageResponse
            {
                Data = ByteString.CopyFrom(mobilePayload),
                ContentType = "image/webp",
                IsStale = false
            }));

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

        // Act
        await service.RefreshCacheAsync();

        // Assert
        memoryCache.TryGetValue("background_image", out BackgroundImageModel? cachedImage).Should().BeTrue();
        cachedImage.Should().NotBeNull();
        cachedImage!.Url.Should().Be("https://www.bing.com/th?id=OHR.Proactive_1920x1080.jpg");

        memoryCache.TryGetValue($"background_image_bytes_desktop_{cachedImage.Version}", out byte[]? desktopBytes).Should().BeTrue();
        desktopBytes.Should().Equal(desktopPayload);

        memoryCache.TryGetValue($"background_image_bytes_mobile_{cachedImage.Version}", out byte[]? mobileBytes).Should().BeTrue();
        mobileBytes.Should().Equal(mobilePayload);
    }

    [Fact]
    public async Task RefreshCacheAsync_WhenGrpcThrowsAndPreviousCacheExists_PreservesExistingCache()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var preCached = new BackgroundImageModel
        {
            Url = "https://example.com/existing.jpg",
            AltText = "Existing Alt",
            Source = ImageOfTheDaySource.Bing
        };
        memoryCache.Set("background_image", preCached);

        mockClient.Setup(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "Transient gRPC outage")));

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

        // Act
        var act = async () => await service.RefreshCacheAsync();

        // Assert: Should not throw and existing cache should remain intact
        await act.Should().NotThrowAsync();
        memoryCache.TryGetValue("background_image", out BackgroundImageModel? currentImage).Should().BeTrue();
        currentImage.Should().BeSameAs(preCached);
    }

    [Fact]
    public async Task GetHeroImageBytesAsync_WhenGrpcFails_ServesStaleBytesIfAvailable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var preCached = new BackgroundImageModel
        {
            Url = "https://www.bing.com/th?id=OHR.Failing_1920x1080.jpg",
            AltText = "Failing Alt",
            Source = ImageOfTheDaySource.Bing
        };
        memoryCache.Set("background_image", preCached);

        var staleSampleBytes = new byte[] { 1, 2, 3, 4, 5 };
        memoryCache.Set("background_image_bytes_desktop_stale", staleSampleBytes);

        mockClient.Setup(c => c.GetImageBytesAsync(
                It.IsAny<HeroImageRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "gRPC unavailable")));

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

        // Act
        var (resultBytes, isStale) = await service.GetHeroImageBytesAsync(DeviceTypes.Desktop);

        // Assert
        resultBytes.Should().NotBeNull();
        resultBytes.Should().Equal(staleSampleBytes);
        isStale.Should().BeTrue();
    }

    [Fact]
    public async Task GetHeroImageBytesAsync_WhenGrpcReturnsStale_ReturnsStaleWithoutCachingAsFresh()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var preCached = new BackgroundImageModel
        {
            Url = "https://www.bing.com/th?id=OHR.StaleGrpc_1920x1080.jpg",
            AltText = "Stale Grpc Alt",
            Source = ImageOfTheDaySource.Bing
        };
        memoryCache.Set("background_image", preCached);

        var staleGrpcBytes = new byte[] { 9, 8, 7 };
        mockClient.Setup(c => c.GetImageBytesAsync(
                It.Is<HeroImageRequest>(r => r.Variant == "desktop"),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(new HeroImageResponse
            {
                Data = ByteString.CopyFrom(staleGrpcBytes),
                ContentType = "image/webp",
                Version = "olderversion",
                IsStale = true
            }));

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);

        // Act
        var (resultBytes, isStale) = await service.GetHeroImageBytesAsync(DeviceTypes.Desktop);

        // Assert
        resultBytes.Should().Equal(staleGrpcBytes);
        isStale.Should().BeTrue();
        memoryCache.TryGetValue($"background_image_bytes_desktop_{preCached.Version}", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetFromCacheAsync_ConcurrentRequestsOnCacheMiss_CallsGrpcOnlyOnce()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var grpcResponse = new BackgroundImageDetails
        {
            Url = "https://example.com/stampede-bg.jpg",
            AltText = "Stampede Alt",
            Source = ImageOfTheDaySource.Bing
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
            var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance);
            return await service.GetFromCacheAsync();
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert: All callers receive the image, but gRPC was called only once (no stampede)
        results.Should().HaveCount(10);
        foreach (var result in results)
        {
            result.Should().NotBeNull();
            result.Url.Should().Be("https://example.com/stampede-bg.jpg");
        }

        mockClient.Verify(c => c.GetAsync(It.IsAny<Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
