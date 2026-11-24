using System.Net;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Domain.Entities;
using Portfolio.Grpc;
using Portfolio.Grpc.Security;
using Portfolio.Grpc.Services;
using Portfolio.Infrastructure;
using Portfolio.Test.Shared;
using DomainSource = Portfolio.Domain.Enums.ImageOfTheDaySource;
using GrpcSource = Portfolio.Grpc.ImageOfTheDaySource;

namespace Portfolio.Test.Grpc;

public class BackgroundImageServicesTests
{
    private static BackgroundImageServices CreateService(
        PortfolioDbContext dbContext,
        HttpClient httpClient,
        SafeUrlValidator? validator = null,
        HeroImageProcessor? processor = null)
    {
        var safeValidator = validator ?? new SafeUrlValidator(
            NullLogger<SafeUrlValidator>.Instance,
            (host, ct) => Task.FromResult(new[] { IPAddress.Parse("93.184.215.14") }));

        var heroImageProcessor = processor ?? new HeroImageProcessor(
            httpClient,
            safeValidator,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<HeroImageProcessor>.Instance);

        return new BackgroundImageServices(
            dbContext,
            httpClient,
            safeValidator,
            heroImageProcessor,
            NullLogger<BackgroundImageServices>.Instance);
    }

    [Fact]
    public async Task Get_WhenImageExists_ReturnsLatestValidImageDetails()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        // Older image
        dbContext.DailyImages.Add(new DailyImage
        {
            ImageUrl = "https://example.com/old.jpg",
            AltText = "Old Alt",
            Source = DomainSource.Bing,
            UrlWorks = true,
            DoIPreferToDisplayThis = true
        });

        // Newer image that works
        dbContext.DailyImages.Add(new DailyImage
        {
            ImageUrl = "https://example.com/latest.jpg",
            AltText = "Latest Alt",
            Source = DomainSource.NASA,
            UrlWorks = true,
            DoIPreferToDisplayThis = true
        });

        // Newer image that does NOT work
        dbContext.DailyImages.Add(new DailyImage
        {
            ImageUrl = "https://example.com/broken.jpg",
            AltText = "Broken Alt",
            Source = DomainSource.Bing,
            UrlWorks = false,
            DoIPreferToDisplayThis = true
        });

        // Newer image where DoIPreferToDisplayThis is false
        dbContext.DailyImages.Add(new DailyImage
        {
            ImageUrl = "https://example.com/unpreferred.jpg",
            AltText = "Unpreferred Alt",
            Source = DomainSource.Bing,
            UrlWorks = true,
            DoIPreferToDisplayThis = false
        });

        await dbContext.SaveChangesAsync();

        // Act
        var response = await service.Get(new Empty(), context);

        // Assert
        response.Should().NotBeNull();
        response.Url.Should().Be("https://example.com/latest.jpg");
        response.AltText.Should().Be("Latest Alt");
        response.Source.Should().Be(GrpcSource.Nasa);
    }

    [Fact]
    public async Task Get_WhenNoValidImagesExist_ReturnsEmptyFallback()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        // Act
        var response = await service.Get(new Empty(), context);

        // Assert
        response.Should().NotBeNull();
        response.Url.Should().BeEmpty();
        response.AltText.Should().BeEmpty();
        response.Source.Should().Be(GrpcSource.None);
    }

    [Fact]
    public async Task Persist_WhenUrlWorks_SavesWithUrlWorksTrueAndDisplayTrue()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = "https://example.com/valid.jpg",
            AltText = "Valid Image",
            Source = GrpcSource.Bing
        };

        // Act
        var response = await service.Persist(request, context);

        // Assert
        response.Should().NotBeNull();
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.ImageUrl == request.Url);
        saved.Should().NotBeNull();
        saved!.UrlWorks.Should().BeTrue();
        saved.DoIPreferToDisplayThis.Should().BeTrue();
        saved.AltText.Should().Be("Valid Image");
        saved.Source.Should().Be(DomainSource.Bing);
    }

    [Fact]
    public async Task Persist_WhenUrlFailsHeadCheck_SavesWithUrlWorksFalseAndDisplayFalse()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = "https://example.com/broken.jpg",
            AltText = "Broken Image",
            Source = GrpcSource.Bing
        };

        // Act
        var response = await service.Persist(request, context);

        // Assert
        response.Should().NotBeNull();
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.ImageUrl == request.Url);
        saved.Should().NotBeNull();
        saved!.UrlWorks.Should().BeFalse();
        saved.DoIPreferToDisplayThis.Should().BeFalse();
    }

    [Fact]
    public async Task Persist_WhenHttpClientThrows_SavesWithUrlWorksFalseAndDisplayFalse()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = "https://example.com/unreachable.jpg",
            AltText = "Unreachable Image",
            Source = GrpcSource.Bing
        };

        // Act
        var response = await service.Persist(request, context);

        // Assert
        response.Should().NotBeNull();
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.ImageUrl == request.Url);
        saved.Should().NotBeNull();
        saved!.UrlWorks.Should().BeFalse();
        saved.DoIPreferToDisplayThis.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "Empty Url")]
    [InlineData("   ", "Whitespace Url")]
    public async Task Persist_WhenUrlIsNullOrEmptyOrWhitespace_SavesWithUrlWorksFalse(string url, string altText)
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = url,
            AltText = altText,
            Source = GrpcSource.None
        };

        // Act
        var response = await service.Persist(request, context);

        // Assert
        response.Should().NotBeNull();
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.AltText == altText);
        saved.Should().NotBeNull();
        saved!.UrlWorks.Should().BeFalse();
        saved.DoIPreferToDisplayThis.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://127.0.0.1:8080/image.jpg")]
    [InlineData("http://192.168.1.120/image.jpg")]
    [InlineData("http://10.244.1.5/image.jpg")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://localhost/image.jpg")]
    [InlineData("file:///etc/passwd")]
    public async Task Persist_WhenUrlIsInternalOrSsrfVector_ThrowsInvalidArgumentRpcException(string url)
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var safeValidator = new SafeUrlValidator(NullLogger<SafeUrlValidator>.Instance);
        var service = CreateService(dbContext, httpClient, safeValidator);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = url,
            AltText = "SSRF Test",
            Source = GrpcSource.Bing
        };

        // Act
        var act = () => service.Persist(request, context);

        // Assert
        var thrown = await act.Should().ThrowAsync<RpcException>();
        thrown.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
        thrown.Which.Status.Detail.Should().Contain("allowed public HTTP or HTTPS endpoint");
        (await dbContext.DailyImages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Persist_WhenUrlRedirectsToPublicUrl_FollowsRedirectAndSavesWithUrlWorksTrue()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri?.AbsoluteUri == "https://example.com/redirect.jpg")
            {
                var redirect = new HttpResponseMessage(HttpStatusCode.Found);
                redirect.Headers.Location = new Uri("https://cdn.example.com/final.jpg");
                return redirect;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = "https://example.com/redirect.jpg",
            AltText = "Redirected Public Image",
            Source = GrpcSource.Bing
        };

        // Act
        await service.Persist(request, context);

        // Assert
        handler.SentRequests.Should().HaveCount(2);
        handler.SentRequests[1].RequestUri!.AbsoluteUri.Should().Be("https://cdn.example.com/final.jpg");
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.ImageUrl == request.Url);
        saved.Should().NotBeNull();
        saved!.UrlWorks.Should().BeTrue();
        saved.DoIPreferToDisplayThis.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://127.0.0.1:8080/admin")]
    [InlineData("http://192.168.1.120/secret")]
    [InlineData("http://10.96.0.1/metrics")]
    public async Task Persist_WhenUrlRedirectsToInternalIp_BlocksRedirectWithoutProbingTarget(string internalTarget)
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(_ =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri(internalTarget);
            return redirect;
        });
        var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = "https://example.com/ssrf-redirect.jpg",
            AltText = "SSRF Redirect",
            Source = GrpcSource.Bing
        };

        // Act
        await service.Persist(request, context);

        // Assert
        handler.SentRequests.Should().HaveCount(1);
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.ImageUrl == request.Url);
        saved.Should().NotBeNull();
        saved!.UrlWorks.Should().BeFalse();
        saved.DoIPreferToDisplayThis.Should().BeFalse();
    }

    [Fact]
    public async Task Persist_WhenRedirectLoopExceedsMaxRedirects_SavesWithUrlWorksFalse()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(_ =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("https://example.com/loop.jpg");
            return redirect;
        });
        var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = "https://example.com/loop.jpg",
            AltText = "Redirect Loop",
            Source = GrpcSource.Bing
        };

        // Act
        await service.Persist(request, context);

        // Assert
        handler.SentRequests.Should().HaveCount(6); // initial + 5 hops
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.ImageUrl == request.Url);
        saved.Should().NotBeNull();
        saved!.UrlWorks.Should().BeFalse();
        saved.DoIPreferToDisplayThis.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSafeHttpHandler_DisablesAutoRedirectAndBlocksDnsRebindingToPrivateIp()
    {
        // Arrange
        var validator = new SafeUrlValidator(
            NullLogger<SafeUrlValidator>.Instance,
            (_, _) => Task.FromResult(new[] { IPAddress.Parse("127.0.0.1") }));

        using var handler = validator.CreateSafeHttpHandler();
        using var client = new HttpClient(handler);

        // Assert handler setting
        handler.AllowAutoRedirect.Should().BeFalse();

        // Act & Assert connection-level DNS rebinding check
        var act = () => client.GetAsync("http://rebind.example.com/test");
        var thrown = await act.Should().ThrowAsync<HttpRequestException>();
        thrown.Which.Message.Should().Contain("private or reserved IP");
    }

    [Fact]
    public async Task GetImageBytes_WhenNoValidImagesExist_ReturnsEmptyData()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        // Act
        var response = await service.GetImageBytes(new HeroImageRequest { Variant = "desktop" }, context);

        // Assert
        response.Should().NotBeNull();
        response.Data.IsEmpty.Should().BeTrue();
        response.ContentType.Should().BeEmpty();
        response.Version.Should().Be("default");
        response.IsStale.Should().BeFalse();
    }

    [Fact]
    public async Task GetImageBytes_WhenValidImageExists_ReturnsEncodedWebpBytesAndVersion()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.DailyImages.Add(new DailyImage
        {
            ImageUrl = "https://example.com/hero.jpg",
            AltText = "Hero Alt",
            Source = DomainSource.Bing,
            UrlWorks = true,
            DoIPreferToDisplayThis = true
        });
        await dbContext.SaveChangesAsync();

        using var bitmap = new SkiaSharp.SKBitmap(64, 64);
        bitmap.Erase(SkiaSharp.SKColors.Teal);
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var jpeg = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 80);
        var jpegBytes = jpeg.ToArray();

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(jpegBytes)
            {
                Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg") }
            }
        });
        using var httpClient = new HttpClient(handler);
        var service = CreateService(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        // Act
        var response = await service.GetImageBytes(new HeroImageRequest { Variant = "desktop" }, context);

        // Assert
        response.Should().NotBeNull();
        response.Data.IsEmpty.Should().BeFalse();
        response.ContentType.Should().Be("image/webp");
        response.Version.Should().Be(Portfolio.Infrastructure.Helpers.ImageVersionHelper.ComputeVersionHash("https://example.com/hero.jpg"));
        response.IsStale.Should().BeFalse();
    }
}
