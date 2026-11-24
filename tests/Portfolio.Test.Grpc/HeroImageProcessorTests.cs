using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Grpc.Security;
using Portfolio.Grpc.Services;
using Portfolio.Infrastructure.Constants;
using Portfolio.Infrastructure.Helpers;
using Portfolio.Test.Shared;
using SkiaSharp;

namespace Portfolio.Test.Grpc;

public class HeroImageProcessorTests
{
    private static SafeUrlValidator CreatePublicValidator() =>
        new(NullLogger<SafeUrlValidator>.Instance,
            (_, _) => Task.FromResult(new[] { IPAddress.Parse("93.184.215.14") }));

    private static byte[] CreateTestJpeg(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 80);
        return encoded.ToArray();
    }

    [Fact]
    public async Task GetEncodedImageAsync_WhenBingMobileVariant_DownloadsMobileUrlEncodesWebpAndCaches()
    {
        // Arrange
        var jpegBytes = CreateTestJpeg(100, 100, SKColors.CornflowerBlue);
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(jpegBytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("image/jpeg") }
            }
        });
        using var httpClient = new HttpClient(handler);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var processor = new HeroImageProcessor(httpClient, CreatePublicValidator(), memoryCache, NullLogger<HeroImageProcessor>.Instance);
        const string url = "https://www.bing.com/th?id=OHR.Test_1920x1080.jpg";

        // Act
        var first = await processor.GetEncodedImageAsync(url, DeviceTypes.Mobile);
        var second = await processor.GetEncodedImageAsync(url, DeviceTypes.Mobile);

        // Assert
        first.Bytes.Should().NotBeNullOrEmpty();
        first.IsStale.Should().BeFalse();
        first.Version.Should().Be(ImageVersionHelper.ComputeVersionHash(url));
        second.Bytes.Should().BeSameAs(first.Bytes);
        second.IsStale.Should().BeFalse();

        handler.SentRequests.Should().ContainSingle();
        handler.SentRequests[0].RequestUri!.ToString().Should().Contain("_768x1280.jpg");
    }

    [Fact]
    public async Task GetEncodedImageAsync_WhenMobileVariantFallsBackTo1080pSource_CropsWithoutUpscaling()
    {
        // Arrange
        var jpegBytes = CreateTestJpeg(1920, 1080, SKColors.SteelBlue);
        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri!.ToString().Contains("_768x1280.jpg"))
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(jpegBytes)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("image/jpeg") }
                }
            };
        });
        using var httpClient = new HttpClient(handler);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var processor = new HeroImageProcessor(httpClient, CreatePublicValidator(), memoryCache, NullLogger<HeroImageProcessor>.Instance);

        // Act
        var result = await processor.GetEncodedImageAsync("https://www.bing.com/th?id=OHR.FallbackCrop_1920x1080.jpg", DeviceTypes.Mobile);

        // Assert
        result.Bytes.Should().NotBeNullOrEmpty();
        result.IsStale.Should().BeFalse();
        using var decoded = SKBitmap.Decode(result.Bytes);
        decoded.Should().NotBeNull();
        decoded!.Width.Should().Be(648);
        decoded.Height.Should().Be(1080);
    }

    [Fact]
    public async Task GetEncodedImageAsync_WhenResponseExceedsSizeLimit_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ =>
        {
            var content = new ByteArrayContent([1, 2, 3, 4]);
            content.Headers.ContentLength = 15 * 1024 * 1024;
            content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });
        using var httpClient = new HttpClient(handler);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var processor = new HeroImageProcessor(httpClient, CreatePublicValidator(), memoryCache, NullLogger<HeroImageProcessor>.Instance);

        // Act
        var result = await processor.GetEncodedImageAsync("https://www.bing.com/th?id=OHR.Oversized_1920x1080.jpg", DeviceTypes.Desktop);

        // Assert
        result.Bytes.Should().BeNull();
        result.IsStale.Should().BeFalse();
    }

    [Fact]
    public async Task GetEncodedImageAsync_WhenContentTypeIsNotImage_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3, 4])
            {
                Headers = { ContentType = new MediaTypeHeaderValue("text/html") }
            }
        });
        using var httpClient = new HttpClient(handler);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var processor = new HeroImageProcessor(httpClient, CreatePublicValidator(), memoryCache, NullLogger<HeroImageProcessor>.Instance);

        // Act
        var result = await processor.GetEncodedImageAsync("https://example.com/not-an-image", DeviceTypes.Desktop);

        // Assert
        result.Bytes.Should().BeNull();
        result.IsStale.Should().BeFalse();
    }

    [Fact]
    public async Task GetEncodedImageAsync_WhenDownloadFails_ServesStaleBytesIfAvailable()
    {
        // Arrange
        var jpegBytes = CreateTestJpeg(100, 100, SKColors.DarkSeaGreen);
        var shouldFail = false;
        var handler = new MockHttpMessageHandler(_ =>
        {
            if (shouldFail)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(jpegBytes)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("image/jpeg") }
                }
            };
        });
        using var httpClient = new HttpClient(handler);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var processor = new HeroImageProcessor(httpClient, CreatePublicValidator(), memoryCache, NullLogger<HeroImageProcessor>.Instance);

        const string initialUrl = "https://www.bing.com/th?id=OHR.Initial_1920x1080.jpg";
        var initial = await processor.GetEncodedImageAsync(initialUrl, DeviceTypes.Desktop);
        initial.Bytes.Should().NotBeNullOrEmpty();

        // Act: Subsequent URL fails to download
        shouldFail = true;
        var stale = await processor.GetEncodedImageAsync("https://www.bing.com/th?id=OHR.Failing_1920x1080.jpg", DeviceTypes.Desktop);

        // Assert
        stale.Bytes.Should().NotBeNull();
        stale.Bytes.Should().Equal(initial.Bytes);
        stale.IsStale.Should().BeTrue();
        stale.Version.Should().Be(initial.Version);
    }

    [Theory]
    [InlineData("http://127.0.0.1/image.jpg")]
    [InlineData("http://192.168.1.120/image.jpg")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    public async Task GetEncodedImageAsync_WhenUrlIsUnsafe_BlocksDownloadWithoutSendingRequest(string unsafeUrl)
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var validator = new SafeUrlValidator(NullLogger<SafeUrlValidator>.Instance);
        var processor = new HeroImageProcessor(httpClient, validator, memoryCache, NullLogger<HeroImageProcessor>.Instance);

        // Act
        var result = await processor.GetEncodedImageAsync(unsafeUrl, DeviceTypes.Desktop);

        // Assert
        result.Bytes.Should().BeNull();
        result.IsStale.Should().BeFalse();
        handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEncodedImageAsync_WhenUrlRedirectsToPrivateIp_BlocksRedirect()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("http://127.0.0.1:8080/secret");
            return redirect;
        });
        using var httpClient = new HttpClient(handler);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var processor = new HeroImageProcessor(httpClient, CreatePublicValidator(), memoryCache, NullLogger<HeroImageProcessor>.Instance);

        // Act
        var result = await processor.GetEncodedImageAsync("https://example.com/redirect.jpg", DeviceTypes.Desktop);

        // Assert
        result.Bytes.Should().BeNull();
        handler.SentRequests.Should().ContainSingle();
    }

    [Fact]
    public async Task GetEncodedImageAsync_WhenUrlRedirectsToPublicUrl_FollowsRedirectAndEncodesWebp()
    {
        // Arrange
        var jpegBytes = CreateTestJpeg(100, 100, SKColors.Coral);
        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsoluteUri == "https://example.com/start.jpg")
            {
                var redirect = new HttpResponseMessage(HttpStatusCode.Found);
                redirect.Headers.Location = new Uri("https://cdn.example.com/final.jpg");
                return redirect;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(jpegBytes)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("image/jpeg") }
                }
            };
        });
        using var httpClient = new HttpClient(handler);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var processor = new HeroImageProcessor(httpClient, CreatePublicValidator(), memoryCache, NullLogger<HeroImageProcessor>.Instance);

        // Act
        var result = await processor.GetEncodedImageAsync("https://example.com/start.jpg", DeviceTypes.Desktop);

        // Assert
        result.Bytes.Should().NotBeNullOrEmpty();
        result.IsStale.Should().BeFalse();
        handler.SentRequests.Should().HaveCount(2);
        handler.SentRequests[1].RequestUri!.AbsoluteUri.Should().Be("https://cdn.example.com/final.jpg");
    }
}
