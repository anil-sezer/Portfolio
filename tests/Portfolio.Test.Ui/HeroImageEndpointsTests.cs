using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portfolio.Grpc;
using Portfolio.Infrastructure.Constants;
using Portfolio.Ui.Endpoints;
using Portfolio.Ui.Models;
using Portfolio.Ui.Services;

namespace Portfolio.Test.Ui;

public class HeroImageEndpointsTests : IDisposable
{
    private readonly string _tempWebRoot;

    public HeroImageEndpointsTests()
    {
        _tempWebRoot = Path.Combine(Path.GetTempPath(), "HeroImageEndpointsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempWebRoot, "img"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempWebRoot))
        {
            Directory.Delete(_tempWebRoot, recursive: true);
        }
    }

    private static Mock<BackgroundImageService> CreateMockBackgroundImageService()
    {
        var mockGrpcClient = new Mock<BackgroundImages.BackgroundImagesClient>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        return new Mock<BackgroundImageService>(
            mockGrpcClient.Object,
            memoryCache,
            NullLogger<BackgroundImageService>.Instance,
            new HttpClient());
    }

    [Fact]
    public async Task ServeHeroImage_WhenBytesNullAndFallbackExists_ServesFallbackWithShortCacheAndNoImmutable()
    {
        // Arrange: Create fallback file on disk
        var fallbackPath = Path.Combine(_tempWebRoot, "img", "default-bg.webp");
        await File.WriteAllBytesAsync(fallbackPath, [1, 2, 3, 4]);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        var mockService = CreateMockBackgroundImageService();
        var model = new BackgroundImageModel
        {
            Url = "https://example.com/photo.jpg",
            AltText = "Test Alt",
            Source = ImageOfTheDaySource.Bing
        };
        mockService.Setup(s => s.GetFromCacheAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        mockService.Setup(s => s.GetHeroImageBytesAsync(DeviceTypes.Desktop, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((byte[]?)null, false));

        var httpContext = new DefaultHttpContext();

        // Act: Request with a version query param
        var result = await HeroImageEndpoints.ServeHeroImage(
            DeviceTypes.Desktop,
            "some-stale-hash",
            mockService.Object,
            mockEnv.Object,
            httpContext,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<PhysicalFileHttpResult>();
        var fileResult = (PhysicalFileHttpResult)result;
        fileResult.FileName.Should().Be(fallbackPath);
        fileResult.ContentType.Should().Be("image/webp");

        var cacheControl = httpContext.Response.Headers.CacheControl.ToString();
        cacheControl.Should().Be("public, max-age=300");
        cacheControl.Should().NotContain("immutable");
    }

    [Fact]
    public async Task ServeHeroImage_WhenBytesExistAndVersionMatches_ReturnsBytesWithImmutableCache()
    {
        // Arrange
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        var mockService = CreateMockBackgroundImageService();
        var model = new BackgroundImageModel
        {
            Url = "https://example.com/photo.jpg",
            AltText = "Test Alt",
            Source = ImageOfTheDaySource.Bing
        };
        var validBytes = new byte[] { 10, 20, 30 };

        mockService.Setup(s => s.GetFromCacheAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        mockService.Setup(s => s.GetHeroImageBytesAsync(DeviceTypes.Desktop, It.IsAny<CancellationToken>()))
            .ReturnsAsync((validBytes, false));

        var httpContext = new DefaultHttpContext();

        // Act: Request with the exact matching version
        var result = await HeroImageEndpoints.ServeHeroImage(
            DeviceTypes.Desktop,
            model.Version,
            mockService.Object,
            mockEnv.Object,
            httpContext,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<FileContentHttpResult>();
        var bytesResult = (FileContentHttpResult)result;
        bytesResult.FileContents.ToArray().Should().Equal(validBytes);
        bytesResult.ContentType.Should().Be("image/webp");

        var cacheControl = httpContext.Response.Headers.CacheControl.ToString();
        cacheControl.Should().Be("public, max-age=31536000, immutable");
    }

    [Fact]
    public async Task ServeHeroImage_WhenBytesExistAndVersionMismatched_ReturnsBytesWithShortCacheAndNoImmutable()
    {
        // Arrange
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        var mockService = CreateMockBackgroundImageService();
        var model = new BackgroundImageModel
        {
            Url = "https://example.com/photo.jpg",
            AltText = "Test Alt",
            Source = ImageOfTheDaySource.Bing
        };
        var validBytes = new byte[] { 10, 20, 30 };

        mockService.Setup(s => s.GetFromCacheAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        mockService.Setup(s => s.GetHeroImageBytesAsync(DeviceTypes.Desktop, It.IsAny<CancellationToken>()))
            .ReturnsAsync((validBytes, false));

        var httpContext = new DefaultHttpContext();

        // Act: Request with an outdated or arbitrary version query param
        var result = await HeroImageEndpoints.ServeHeroImage(
            DeviceTypes.Desktop,
            "mismatched_hash",
            mockService.Object,
            mockEnv.Object,
            httpContext,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<FileContentHttpResult>();
        var bytesResult = (FileContentHttpResult)result;
        bytesResult.FileContents.ToArray().Should().Equal(validBytes);

        var cacheControl = httpContext.Response.Headers.CacheControl.ToString();
        cacheControl.Should().Be("public, max-age=300");
        cacheControl.Should().NotContain("immutable");
    }

    [Fact]
    public async Task ServeHeroImage_WhenBytesExistAndVersionOmitted_ReturnsBytesWithStandardCache()
    {
        // Arrange
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        var mockService = CreateMockBackgroundImageService();
        var model = new BackgroundImageModel
        {
            Url = "https://example.com/photo.jpg",
            AltText = "Test Alt",
            Source = ImageOfTheDaySource.Bing
        };
        var validBytes = new byte[] { 10, 20, 30 };

        mockService.Setup(s => s.GetFromCacheAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        mockService.Setup(s => s.GetHeroImageBytesAsync(DeviceTypes.Desktop, It.IsAny<CancellationToken>()))
            .ReturnsAsync((validBytes, false));

        var httpContext = new DefaultHttpContext();

        // Act: Request without version query param (unversioned URL)
        var result = await HeroImageEndpoints.ServeHeroImage(
            DeviceTypes.Desktop,
            null,
            mockService.Object,
            mockEnv.Object,
            httpContext,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<FileContentHttpResult>();
        var bytesResult = (FileContentHttpResult)result;
        bytesResult.FileContents.ToArray().Should().Equal(validBytes);

        var cacheControl = httpContext.Response.Headers.CacheControl.ToString();
        cacheControl.Should().Be("public, max-age=86400");
        cacheControl.Should().NotContain("immutable");
    }

    [Fact]
    public async Task ServeHeroImage_WhenBytesNullAndFallbackFileNotFound_ReturnsNotFound()
    {
        // Arrange: No fallback file created in _tempWebRoot
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        var mockService = CreateMockBackgroundImageService();
        var model = new BackgroundImageModel
        {
            Url = "https://example.com/photo.jpg",
            AltText = "Test Alt",
            Source = ImageOfTheDaySource.Bing
        };

        mockService.Setup(s => s.GetFromCacheAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        mockService.Setup(s => s.GetHeroImageBytesAsync(DeviceTypes.Desktop, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((byte[]?)null, false));

        var httpContext = new DefaultHttpContext();

        // Act
        var result = await HeroImageEndpoints.ServeHeroImage(
            DeviceTypes.Desktop,
            "some-hash",
            mockService.Object,
            mockEnv.Object,
            httpContext,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task ServeHeroImage_WhenStaleBytesReturnedEvenWithMatchingVersion_ReturnsShortCacheAndNoImmutable()
    {
        // Arrange
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        var mockService = CreateMockBackgroundImageService();
        var model = new BackgroundImageModel
        {
            Url = "https://example.com/photo.jpg",
            AltText = "Test Alt",
            Source = ImageOfTheDaySource.Bing
        };
        var staleBytes = new byte[] { 10, 20, 30 };

        mockService.Setup(s => s.GetFromCacheAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        mockService.Setup(s => s.GetHeroImageBytesAsync(DeviceTypes.Desktop, It.IsAny<CancellationToken>()))
            .ReturnsAsync((staleBytes, true));

        var httpContext = new DefaultHttpContext();

        // Act: Request with matching version, but service returned stale fallback bytes
        var result = await HeroImageEndpoints.ServeHeroImage(
            DeviceTypes.Desktop,
            model.Version,
            mockService.Object,
            mockEnv.Object,
            httpContext,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<FileContentHttpResult>();
        var bytesResult = (FileContentHttpResult)result;
        bytesResult.FileContents.ToArray().Should().Equal(staleBytes);

        var cacheControl = httpContext.Response.Headers.CacheControl.ToString();
        cacheControl.Should().Be("public, max-age=300");
        cacheControl.Should().NotContain("immutable");
    }
}
