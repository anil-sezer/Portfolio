using System.Diagnostics;
using Grpc.Core.Interceptors;

namespace Portfolio.Grpc.Interceptors;

public class ServerLoggingAndExceptionInterceptor(ILogger<ServerLoggingAndExceptionInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("➡️ gRPC starting {Method} from {Peer}", context.Method, context.Peer);

        try
        {
            var response = await continuation(request, context);
            stopwatch.Stop();
            logger.LogInformation("⬅️ gRPC completed {Method} with StatusCode.{StatusCode} in {ElapsedMilliseconds}ms",
                context.Method, context.Status.StatusCode, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (RpcException ex)
        {
            stopwatch.Stop();
            var level = ex.StatusCode is StatusCode.InvalidArgument or StatusCode.NotFound or StatusCode.PermissionDenied
                ? LogLevel.Warning
                : LogLevel.Error;

            logger.Log(level, ex, "⚠️ gRPC call {Method} failed with StatusCode.{StatusCode} ({StatusDetail}) in {ElapsedMilliseconds}ms",
                context.Method, ex.StatusCode, ex.Status.Detail, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (OperationCanceledException ex) when (context.CancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            logger.LogWarning(ex, "⏹️ gRPC call {Method} was cancelled by client after {ElapsedMilliseconds}ms",
                context.Method, stopwatch.ElapsedMilliseconds);
            throw new RpcException(new Status(StatusCode.Cancelled, "Call cancelled by client"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "💥 Unhandled exception during gRPC call {Method} after {ElapsedMilliseconds}ms",
                context.Method, stopwatch.ElapsedMilliseconds);
            throw new RpcException(new Status(StatusCode.Internal, "An internal error occurred while processing the request."));
        }
    }
}
