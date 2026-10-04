using System.Net;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Moq;
using Portfolio.Grpc;
using Portfolio.Test.Shared;
using Portfolio.Ui.Middlewares;
using Portfolio.Ui.Services;

namespace Portfolio.Test.Ui;

public class NotFoundLoggingMiddlewareTests
{
    private static (DefaultHttpContext context, Mock<IHttpContextAccessor> mockAccessor, Mock<VisitorInsights.VisitorInsightsClient> mockClient, LogVisitService logService) CreateContextAndServices(int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = statusCode;
        context.Request.Path = "/non-existent-page";
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(context);

        var mockClient = new Mock<VisitorInsights.VisitorInsightsClient>();
        mockClient.Setup(c => c.StoreVisitorInfoAsync(
                It.IsAny<StoreVisitorInfoRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(new Empty()));

        var logService = new LogVisitService(mockClient.Object);

        return (context, mockAccessor, mockClient, logService);
    }

    [Theory]
    [InlineData(404)]
    [InlineData(302)]
    public async Task InvokeAsync_WhenStatusCodeIs404Or302_TriggersVisitLogging(int statusCode)
    {
        // Arrange
        var (context, mockAccessor, mockClient, logService) = CreateContextAndServices(statusCode);
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next);

        // Act
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.Is<StoreVisitorInfoRequest>(r => r.RequestedUrl.Contains("/non-existent-page")),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(204)]
    [InlineData(500)]
    public async Task InvokeAsync_WhenStatusCodeIsNot404Or302_DoesNotTriggerVisitLogging(int statusCode)
    {
        // Arrange
        var (context, mockAccessor, mockClient, logService) = CreateContextAndServices(statusCode);
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next);

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
        var (context, mockAccessor, mockClient, logService) = CreateContextAndServices(404);
        mockClient.Setup(c => c.StoreVisitorInfoAsync(
                It.IsAny<StoreVisitorInfoRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "Service unavailable")));

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next);

        // Act
        var act = async () => await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsync_WhenLoggingIsCancelled_CatchesOperationCanceledExceptionGracefully()
    {
        // Arrange
        var (context, mockAccessor, mockClient, logService) = CreateContextAndServices(404);
        mockClient.Setup(c => c.StoreVisitorInfoAsync(
                It.IsAny<StoreVisitorInfoRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException());

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next);

        // Act
        var act = async () => await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSetsStatusCodeTo404_TriggersVisitLogging()
    {
        // Arrange: Start with 200, next delegate changes to 404
        var (context, mockAccessor, mockClient, logService) = CreateContextAndServices(200);
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = 404;
            return Task.CompletedTask;
        };
        var middleware = new NotFoundLoggingMiddleware(next);

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
        var (context, mockAccessor, _, logService) = CreateContextAndServices(200);
        RequestDelegate next = _ => throw new InvalidOperationException("Downstream middleware error");
        var middleware = new NotFoundLoggingMiddleware(next);

        // Act
        var act = async () => await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Downstream middleware error");
    }

    [Theory]
    [InlineData(301)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    public async Task InvokeAsync_WhenStatusCodeIsOtherRedirectOrClientError_DoesNotTriggerVisitLogging(int statusCode)
    {
        // Arrange
        var (context, mockAccessor, mockClient, logService) = CreateContextAndServices(statusCode);
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new NotFoundLoggingMiddleware(next);

        // Act
        await middleware.InvokeAsync(context, logService, mockAccessor.Object);

        // Assert
        mockClient.Verify(c => c.StoreVisitorInfoAsync(
            It.IsAny<StoreVisitorInfoRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
