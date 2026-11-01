using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Portfolio.Ui.Middlewares;

namespace Portfolio.Test.Ui;

public class LanguageRedirectMiddlewareTests
{
    [Theory]
    [InlineData("tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7", true)]
    [InlineData("tr-TR", true)]
    [InlineData("tr", true)]
    [InlineData("en-US,en;q=0.9,tr;q=0.8", false)]
    [InlineData("tr;q=0.8,en-US;q=0.9", false)]
    [InlineData("tr;q=0.9,en;q=0.9", true)]
    [InlineData("en;q=0.9,tr;q=0.9", false)]
    [InlineData("tr;q=0", false)]
    [InlineData("de-DE,de;q=0.9,en;q=0.8", false)]
    [InlineData("de-DE,de;q=0.9", false)]
    [InlineData("*", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void PrefersTurkish_EvaluatesHeaderCorrectly(string? acceptLanguage, bool expected)
    {
        var result = LanguageRedirectMiddleware.PrefersTurkish(acceptLanguage);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task InvokeAsync_WhenRootGetAndTurkishAcceptLanguage_RedirectsToTr()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/";
        context.Request.Headers.AcceptLanguage = "tr-TR,tr;q=0.9,en-US;q=0.8";

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LanguageRedirectMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
        context.Response.Headers.Location.ToString().Should().Be("/tr");
    }

    [Fact]
    public async Task InvokeAsync_WhenRootGetAndTurkishAcceptLanguageWithQuery_PreservesQueryString()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/";
        context.Request.QueryString = new QueryString("?utm_source=test&ref=xyz");
        context.Request.Headers.AcceptLanguage = "tr-TR,tr;q=0.9";

        var middleware = new LanguageRedirectMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
        context.Response.Headers.Location.ToString().Should().Be("/tr?utm_source=test&ref=xyz");
    }

    [Fact]
    public async Task InvokeAsync_WhenRootGetAndEnglishAcceptLanguage_DoesNotRedirect()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/";
        context.Request.Headers.AcceptLanguage = "en-US,en;q=0.9";

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LanguageRedirectMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WhenRootGetWithTurkishAcceptLanguageButEnglishCookie_RespectsCookieAndDoesNotRedirect()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/";
        context.Request.Headers.AcceptLanguage = "tr-TR,tr;q=0.9";
        context.Request.Headers.Cookie = $"{CookieRequestCultureProvider.DefaultCookieName}={CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en-US"))}";

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LanguageRedirectMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WhenRootGetWithEnglishAcceptLanguageButTurkishCookie_RespectsCookieAndRedirectsToTr()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/";
        context.Request.Headers.AcceptLanguage = "en-US,en;q=0.9";
        context.Request.Headers.Cookie = $"{CookieRequestCultureProvider.DefaultCookieName}={CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("tr-TR"))}";

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LanguageRedirectMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
        context.Response.Headers.Location.ToString().Should().Be("/tr");
    }

    [Fact]
    public async Task InvokeAsync_WhenVisitingTrPath_EnsuresCultureCookieAndCallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/tr";

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LanguageRedirectMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.Headers.SetCookie.ToString().Should().Contain(CookieRequestCultureProvider.DefaultCookieName);
        Uri.UnescapeDataString(context.Response.Headers.SetCookie.ToString()).Should().Contain("c=tr-TR");
    }

    [Fact]
    public async Task InvokeAsync_WhenVisitingTrPathWithExistingCookie_DoesNotSetDuplicateCookie()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/tr";
        context.Request.Headers.Cookie = $"{CookieRequestCultureProvider.DefaultCookieName}={CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("tr-TR"))}";

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LanguageRedirectMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.Headers.SetCookie.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WhenNonRootPath_CallsNextWithoutRedirect()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/other";
        context.Request.Headers.AcceptLanguage = "tr-TR";

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LanguageRedirectMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
