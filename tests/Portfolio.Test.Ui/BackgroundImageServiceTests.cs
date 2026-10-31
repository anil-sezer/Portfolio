using System.Net;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using Portfolio.Grpc;
using Portfolio.Test.Shared;
using Portfolio.Ui.Models;
using Portfolio.Ui.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Infrastructure.Constants;
using SkiaSharp;

namespace Portfolio.Test.Ui;

[Collection("EnvironmentVariables")]
public class BackgroundImageServiceTests : IDisposable
{
    private readonly string? _origEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    private readonly HttpClient _dummyHttpClient = new();

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _origEnv);
        _dummyHttpClient.Dispose();
    }

    [Fact]
    public async Task GetFromCacheAsync_InDevelopmentMode_ReturnsFallbackWithoutCallingGrpc()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, _dummyHttpClient);

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

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, _dummyHttpClient);

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

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, _dummyHttpClient);

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

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, _dummyHttpClient);

        // Act
        var result = await service.GetFromCacheAsync();

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Contain("default-bg.webp");
        result.Source.Should().Be(ImageOfTheDaySource.Bing);
    }

    [Fact]
    public async Task GetHeroImageBytesAsync_WhenRemoteUrl_DownloadsAndEncodesWebpAndCaches()
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

        using var testBitmap = new SKBitmap(100, 100);
        testBitmap.Erase(SKColors.CornflowerBlue);
        using var testImage = SKImage.FromBitmap(testBitmap);
        using var encodedJpeg = testImage.Encode(SKEncodedImageFormat.Jpeg, 80);
        var jpegBytes = encodedJpeg.ToArray();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(jpegBytes)
            });

        using var httpClient = new HttpClient(handlerMock.Object);
        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, httpClient);

        // Act
        var (mobileBytes, isStale) = await service.GetHeroImageBytesAsync(DeviceTypes.Mobile);
        var (cachedMobileBytes, isCachedStale) = await service.GetHeroImageBytesAsync(DeviceTypes.Mobile);

        // Assert
        mobileBytes.Should().NotBeNullOrEmpty();
        isStale.Should().BeFalse();
        cachedMobileBytes.Should().BeSameAs(mobileBytes);
        isCachedStale.Should().BeFalse();
        preCached.MobileImageUrl.Should().StartWith("/api/hero-image/mobile?v=");
        preCached.DesktopImageUrl.Should().StartWith("/api/hero-image/desktop?v=");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("_768x1280.jpg")),
            ItExpr.IsAny<CancellationToken>());
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

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, _dummyHttpClient);

        // Act
        var act = async () => await service.GetFromCacheAsync();

        // Assert
        await act.Should().ThrowAsync<RpcException>()
            .WithMessage("*gRPC service down*");
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

        using var testBitmap = new SKBitmap(100, 100);
        testBitmap.Erase(SKColors.DarkSeaGreen);
        using var testImage = SKImage.FromBitmap(testBitmap);
        using var encodedJpeg = testImage.Encode(SKEncodedImageFormat.Jpeg, 80);
        var jpegBytes = encodedJpeg.ToArray();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(jpegBytes)
            });

        using var httpClient = new HttpClient(handlerMock.Object);
        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, httpClient);

        // Act
        await service.RefreshCacheAsync();

        // Assert
        memoryCache.TryGetValue("background_image", out BackgroundImageModel? cachedImage).Should().BeTrue();
        cachedImage.Should().NotBeNull();
        cachedImage!.Url.Should().Be("https://www.bing.com/th?id=OHR.Proactive_1920x1080.jpg");

        memoryCache.TryGetValue($"background_image_bytes_desktop_{cachedImage.Version}", out byte[]? desktopBytes).Should().BeTrue();
        desktopBytes.Should().NotBeNullOrEmpty();

        memoryCache.TryGetValue($"background_image_bytes_mobile_{cachedImage.Version}", out byte[]? mobileBytes).Should().BeTrue();
        mobileBytes.Should().NotBeNullOrEmpty();
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

        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, _dummyHttpClient);

        // Act
        var act = async () => await service.RefreshCacheAsync();

        // Assert: Should not throw and existing cache should remain intact
        await act.Should().NotThrowAsync();
        memoryCache.TryGetValue("background_image", out BackgroundImageModel? currentImage).Should().BeTrue();
        currentImage.Should().BeSameAs(preCached);
    }

    [Fact]
    public async Task GetHeroImageBytesAsync_WhenDownloadFails_ServesStaleBytesIfAvailable()
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

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var httpClient = new HttpClient(handlerMock.Object);
        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, httpClient);

        // Act
        var (resultBytes, isStale) = await service.GetHeroImageBytesAsync(DeviceTypes.Desktop);

        // Assert
        resultBytes.Should().NotBeNull();
        resultBytes.Should().Equal(staleSampleBytes);
        isStale.Should().BeTrue();
    }

    [Fact]
    public async Task GetHeroImageBytesAsync_WhenMobileVariantFallsBackTo1080pSource_CropsWithoutUpscaling()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var preCached = new BackgroundImageModel
        {
            Url = "https://www.bing.com/th?id=OHR.FallbackCrop_1920x1080.jpg",
            AltText = "Fallback Crop Alt",
            Source = ImageOfTheDaySource.Bing
        };
        memoryCache.Set("background_image", preCached);

        using var sourceBitmap = new SKBitmap(1920, 1080);
        sourceBitmap.Erase(SKColors.SteelBlue);
        using var sourceImage = SKImage.FromBitmap(sourceBitmap);
        using var encodedJpeg = sourceImage.Encode(SKEncodedImageFormat.Jpeg, 80);
        var jpegBytes = encodedJpeg.ToArray();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("_768x1280.jpg")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.NotFound));

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("_1920x1080.jpg")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(jpegBytes)
            });

        using var httpClient = new HttpClient(handlerMock.Object);
        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, httpClient);

        // Act
        var (mobileBytes, isStale) = await service.GetHeroImageBytesAsync(DeviceTypes.Mobile);

        // Assert
        mobileBytes.Should().NotBeNullOrEmpty();
        isStale.Should().BeFalse();
        using var decoded = SKBitmap.Decode(mobileBytes);
        decoded.Should().NotBeNull();
        decoded!.Width.Should().Be(648);
        decoded.Height.Should().Be(1080);
    }

    [Fact]
    public async Task GetHeroImageBytesAsync_WhenResponseExceedsSizeLimit_ReturnsNull()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var mockClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var preCached = new BackgroundImageModel
        {
            Url = "https://www.bing.com/th?id=OHR.Oversized_1920x1080.jpg",
            AltText = "Oversized Alt",
            Source = ImageOfTheDaySource.Bing
        };
        memoryCache.Set("background_image", preCached);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var content = new ByteArrayContent([1, 2, 3, 4]);
                content.Headers.ContentLength = 15 * 1024 * 1024;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            });

        using var httpClient = new HttpClient(handlerMock.Object);
        var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, httpClient);

        // Act
        var (bytes, isStale) = await service.GetHeroImageBytesAsync(DeviceTypes.Desktop);

        // Assert
        bytes.Should().BeNull();
        isStale.Should().BeFalse();
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
            var service = new BackgroundImageService(mockClient.Object, memoryCache, NullLogger<BackgroundImageService>.Instance, _dummyHttpClient);
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
