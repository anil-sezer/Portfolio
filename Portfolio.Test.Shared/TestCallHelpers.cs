using Grpc.Core;

namespace Portfolio.Test.Shared;

public static class TestCallHelpers
{
    public static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });
    }

    public static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(Task<TResponse> responseTask)
    {
        return new AsyncUnaryCall<TResponse>(
            responseTask,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });
    }
}
