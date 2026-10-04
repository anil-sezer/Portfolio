using System.Net;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Grpc;
using Portfolio.Grpc.Services;
using Portfolio.Test.Shared;
using DomainSource = Portfolio.Domain.Enums.ImageOfTheDaySource;
using GrpcSource = Portfolio.Grpc.ImageOfTheDaySource;

namespace Portfolio.Test.Grpc;

public class BackgroundImageServicesTests
{
    [Fact]
    public async Task Get_WhenImageExists_ReturnsLatestValidImageDetails()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var service = new BackgroundImageServices(dbContext, httpClient);
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
        var service = new BackgroundImageServices(dbContext, httpClient);
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
        var service = new BackgroundImageServices(dbContext, httpClient);
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
        saved.UrlWorks.Should().BeTrue();
        saved.DoIPreferToDisplayThis.Should().BeTrue();
        saved.AltText.Should().Be("Valid Image");
        saved.Source.Should().Be(DomainSource.Bing);
    }

    [Fact]
    public async Task Persist_WhenUrlFails_SavesWithUrlWorksFalseAndDisplayFalse()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler);
        var service = new BackgroundImageServices(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = "https://example.com/missing.jpg",
            AltText = "Missing Image",
            Source = GrpcSource.Bing
        };

        // Act
        var response = await service.Persist(request, context);

        // Assert
        response.Should().NotBeNull();
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.ImageUrl == request.Url);
        saved.Should().NotBeNull();
        saved.UrlWorks.Should().BeFalse();
        saved.DoIPreferToDisplayThis.Should().BeFalse();
    }

    [Fact]
    public async Task Persist_WhenUrlIsEmpty_SavesWithUrlWorksFalse()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var service = new BackgroundImageServices(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = "",
            AltText = "Empty Url",
            Source = GrpcSource.None
        };

        // Act
        var response = await service.Persist(request, context);

        // Assert
        response.Should().NotBeNull();
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.AltText == "Empty Url");
        saved.Should().NotBeNull();
        saved.UrlWorks.Should().BeFalse();
        saved.DoIPreferToDisplayThis.Should().BeFalse();
    }

    [Fact]
    public async Task Persist_WhenHttpRequestThrowsException_SavesWithUrlWorksFalseAndDisplayFalse()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("DNS resolution failed"));
        var httpClient = new HttpClient(handler);
        var service = new BackgroundImageServices(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = "https://unreachable.example.com/image.jpg",
            AltText = "Unreachable",
            Source = GrpcSource.Nasa
        };

        // Act
        var response = await service.Persist(request, context);

        // Assert
        response.Should().NotBeNull();
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.ImageUrl == request.Url);
        saved.Should().NotBeNull();
        saved.UrlWorks.Should().BeFalse();
        saved.DoIPreferToDisplayThis.Should().BeFalse();
        saved.Source.Should().Be(DomainSource.NASA);
    }

    [Fact]
    public async Task Persist_WhenUrlIsWhitespace_SavesWithUrlWorksFalse()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var service = new BackgroundImageServices(dbContext, httpClient);
        var context = TestServerCallContext.Create();

        var request = new BackgroundImageDetails
        {
            Url = "   ",
            AltText = "Whitespace Url",
            Source = GrpcSource.Bing
        };

        // Act
        var response = await service.Persist(request, context);

        // Assert
        response.Should().NotBeNull();
        var saved = await dbContext.DailyImages.FirstOrDefaultAsync(x => x.AltText == "Whitespace Url");
        saved.Should().NotBeNull();
        saved.UrlWorks.Should().BeFalse();
        saved.DoIPreferToDisplayThis.Should().BeFalse();
    }
}
