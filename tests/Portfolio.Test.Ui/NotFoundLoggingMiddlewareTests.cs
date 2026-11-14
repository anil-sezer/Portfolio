using System.Net;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Moq;
using Portfolio.Grpc;
using Portfolio.Test.Shared;
using Portfolio.Ui.Middlewares;
using Portfolio.Ui.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Portfolio.Test.Ui;

public class NotFoundLoggingMiddlewareTests
{
    private sealed class FakeSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private static (DefaultHttpContext context, Mock<IHttpContextAccessor> mockAccessor, Mock<VisitorInsights.VisitorInsightsClient> mockClient, LogVisitService logService, MemoryCache memoryCache) CreateContextAndServices(
        int statusCode,
        string path = "/non-existent-page",
        string ipAddress = "203.0.113.195",
        ISystemClock? clock = null)
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = statusCode;
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);

        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(context);

        var mockClient = new Mock<VisitorInsights.VisitorInsightsClient>();
        mockClient.Setup(c => c.StoreVisitorInfoAsync(
                It.IsAny<StoreVisitorInfoRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(new Empty()));

        var logService = new LogVisitService(mockClient.Object, NullLogger<LogVisitService>.Instance);
        var memoryCache = new MemoryCache(new MemoryCacheOptions { Clock = clock });

        return (context, mockAccessor, mockClient, logService, memoryCache);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(301)]
    [InlineData(500)]
    public async Task InvokeAsync_WhenStatusCodeIsNot404_DoesNotTriggerVisitLogging(int statusCode)
    {
        // Arrange
        var (context, mockAccessor, mockClient, logService, memoryCache) = CreateContextAndServices(statusCode);
        using var _ = memoryCache;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.IsAny<StoreVisitorInfoRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenStatusCodeReExecuteFeaturePresent_SkipsDuplicateLogging()
    {
        // Arrange
        var (context, mockAccessor, mockClient, logService, memoryCache) = CreateContextAndServices(404);
        using var _ = memoryCache;
        var mockReExecuteFeature = new Mock<IStatusCodeReExecuteFeature>();
        mockReExecuteFeature.Setup(f => f.OriginalPath).Returns("/asd");
        context.Features.Set<IStatusCodeReExecuteFeature>(mockReExecuteFeature.Object);

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.IsAny<StoreVisitorInfoRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenLoggingThrowsException_CatchesGracefullyWithoutThrowing()
    {
        // Arrange
        var (context, mockAccessor, mockClient, logService, memoryCache) = CreateContextAndServices(404);
        using var _ = memoryCache;
        mockClient.Setup(c => c.StoreVisitorInfoAsync(
                It.IsAny<StoreVisitorInfoRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "Service unavailable")));

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        var act = async () => await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsync_WhenLoggingIsCancelled_CatchesOperationCanceledExceptionGracefully()
    {
        // Arrange
        var (context, mockAccessor, mockClient, logService, memoryCache) = CreateContextAndServices(404);
        using var _ = memoryCache;
        mockClient.Setup(c => c.StoreVisitorInfoAsync(
                It.IsAny<StoreVisitorInfoRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException());

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        var act = async () => await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSetsStatusCodeTo404_TriggersVisitLogging()
    {
        // Arrange: Start with 200, next delegate changes to 404
        var (context, mockAccessor, mockClient, logService, memoryCache) = CreateContextAndServices(200);
        using var _ = memoryCache;
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = 404;
            return Task.CompletedTask;
        };
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.Is<StoreVisitorInfoRequest>(r => r.RequestedUrl.Contains("/non-existent-page")),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrowsException_DoesNotSwallowAndRethrows()
    {
        // Arrange
        var (context, mockAccessor, _, logService, memoryCache) = CreateContextAndServices(200);
        using var _ = memoryCache;
        RequestDelegate next = _ => throw new InvalidOperationException("Downstream middleware error");
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        var act = async () => await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Downstream middleware error");
    }

    [Fact]
    public async Task InvokeAsync_WhenClientIsLoopback_DoesNotTriggerVisitLogging()
    {
        // Arrange
        var (context, mockAccessor, mockClient, logService, memoryCache) = CreateContextAndServices(404, ipAddress: "127.0.0.1");
        using var _ = memoryCache;

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.IsAny<StoreVisitorInfoRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("/css/missing.css")]
    [InlineData("/js/missing.js")]
    [InlineData("/_framework/missing.wasm")]
    [InlineData("/favicon.ico")]
    [InlineData("/images/missing.png")]
    [InlineData("/images/missing.avif")]
    public async Task InvokeAsync_WhenPathIsStaticAsset_SkipsVisitLogging(string assetPath)
    {
        // Arrange
        var (context, mockAccessor, mockClient, logService, memoryCache) = CreateContextAndServices(404, path: assetPath);
        using var _ = memoryCache;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.IsAny<StoreVisitorInfoRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenSameClientRequestsSame404PathRepeatedly_LogsOnlyOnce()
    {
        // Arrange
        var (context1, mockAccessor1, mockClient, logService, memoryCache) = CreateContextAndServices(404, path: "/Missing-Page");
        using var _ = memoryCache;
        context1.Request.QueryString = new QueryString("?cb=1");

        var context2 = new DefaultHttpContext();
        context2.Response.StatusCode = 404;
        context2.Request.Path = "/missing-page";
        context2.Request.QueryString = new QueryString("?cb=2");
        context2.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.195");
        var mockAccessor2 = new Mock<IHttpContextAccessor>();
        mockAccessor2.Setup(a => a.HttpContext).Returns(context2);

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        await middleware.InvokeAsync(context1, logService, mockAccessor1.Object);
        await middleware.InvokeAsync(context2, logService, mockAccessor2.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.IsAny<StoreVisitorInfoRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenDifferentClientsRequestSame404Path_LogsOncePerClient()
    {
        // Arrange
        var (context1, mockAccessor1, mockClient, logService, memoryCache) = CreateContextAndServices(404, path: "/missing", ipAddress: "203.0.113.10");
        using var _ = memoryCache;

        var context2 = new DefaultHttpContext();
        context2.Response.StatusCode = 404;
        context2.Request.Path = "/missing";
        context2.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.20");
        var mockAccessor2 = new Mock<IHttpContextAccessor>();
        mockAccessor2.Setup(a => a.HttpContext).Returns(context2);

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        await middleware.InvokeAsync(context1, logService, mockAccessor1.Object);
        await middleware.InvokeAsync(context2, logService, mockAccessor2.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.IsAny<StoreVisitorInfoRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task InvokeAsync_WhenSameClientExceedsPerIpLimitWithDistinctPaths_ThrottlesSubsequent404s()
    {
        // Arrange
        var (_, _, mockClient, logService, memoryCache) = CreateContextAndServices(404);
        using var _ = memoryCache;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        var totalRequests = NotFoundLoggingMiddleware.Max404LogsPerIpPerWindow + 5;

        // Act
        for (var i = 0; i < totalRequests; i++)
        {
            var ctx = new DefaultHttpContext();
            ctx.Response.StatusCode = 404;
            ctx.Request.Path = $"/scan-path-{i}";
            ctx.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.50");

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(ctx);

            await middleware.InvokeAsync(ctx, logService, accessor.Object);
        }

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.IsAny<StoreVisitorInfoRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Exactly(NotFoundLoggingMiddleware.Max404LogsPerIpPerWindow));
    }

    [Fact]
    public async Task InvokeAsync_WhenCacheEntryExpires_LogsSame404PathAgain()
    {
        // Arrange
        var clock = new FakeSystemClock(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));
        var (context, mockAccessor, mockClient, logService, memoryCache) = CreateContextAndServices(404, path: "/expired-test", clock: clock);
        using var _ = memoryCache;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act: first request logged, second within window skipped, third after expiration logged again
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        clock.UtcNow += NotFoundLoggingMiddleware.CacheWindow + TimeSpan.FromSeconds(1);
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.IsAny<StoreVisitorInfoRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task InvokeAsync_WhenPathExceedsMaxRawKeyLength_DeduplicatesUsingHash()
    {
        // Arrange
        var longPath = "/" + new string('a', 300);
        var (context, mockAccessor, mockClient, logService, memoryCache) = CreateContextAndServices(404, path: longPath);
        using var _ = memoryCache;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next, NullLogger<NotFoundLoggingMiddleware>.Instance, memoryCache);

        // Act
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.IsAny<StoreVisitorInfoRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
