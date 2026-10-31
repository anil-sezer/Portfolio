using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Grpc.Interceptors;
using Portfolio.Test.Shared;

namespace Portfolio.Test.Grpc.Interceptors;

public class ServerLoggingAndExceptionInterceptorTests
{
    private readonly ServerLoggingAndExceptionInterceptor _interceptor =
        new(NullLogger<ServerLoggingAndExceptionInterceptor>.Instance);

    [Fact]
    public async Task UnaryServerHandler_WhenContinuationSucceeds_ReturnsResponse()
    {
        // Arrange
        var context = TestServerCallContext.Create();
        var request = "hello";

        // Act
        var result = await _interceptor.UnaryServerHandler(
            request,
            context,
            (req, ctx) => Task.FromResult($"{req} world"));

        // Assert
        result.Should().Be("hello world");
    }

    [Fact]
    public async Task UnaryServerHandler_WhenRpcExceptionThrown_RethrowsSameRpcException()
    {
        // Arrange
        var context = TestServerCallContext.Create();
        var originalException = new RpcException(new Status(StatusCode.PermissionDenied, "Access denied"));

        // Act
        var act = () => _interceptor.UnaryServerHandler<string, string>(
            "test",
            context,
            (req, ctx) => throw originalException);

        // Assert
        var thrown = await act.Should().ThrowAsync<RpcException>();
        thrown.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
        thrown.Which.Status.Detail.Should().Be("Access denied");
    }

    [Fact]
    public async Task UnaryServerHandler_WhenOperationCanceled_ThrowsCancelledRpcException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var context = TestServerCallContext.Create(cancellationToken: cts.Token);

        // Act
        var act = () => _interceptor.UnaryServerHandler<string, string>(
            "test",
            context,
            (req, ctx) => throw new OperationCanceledException(cts.Token));

        // Assert
        var thrown = await act.Should().ThrowAsync<RpcException>();
        thrown.Which.StatusCode.Should().Be(StatusCode.Cancelled);
    }

    [Fact]
    public async Task UnaryServerHandler_WhenUnhandledExceptionThrown_TranslatesToInternalRpcException()
    {
        // Arrange
        var context = TestServerCallContext.Create();

        // Act
        var act = () => _interceptor.UnaryServerHandler<string, string>(
            "test",
            context,
            (req, ctx) => throw new InvalidOperationException("Sensitive DB connection string details"));

        // Assert
        var thrown = await act.Should().ThrowAsync<RpcException>();
        thrown.Which.StatusCode.Should().Be(StatusCode.Internal);
        thrown.Which.Status.Detail.Should().Be("An internal error occurred while processing the request.");
        thrown.Which.Status.Detail.Should().NotContain("Sensitive DB connection");
    }
}
