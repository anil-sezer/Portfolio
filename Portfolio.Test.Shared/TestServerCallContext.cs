using Grpc.Core;

namespace Portfolio.Test.Shared;

public class TestServerCallContext : ServerCallContext
{
    private readonly Metadata _requestHeaders;
    private readonly CancellationToken _cancellationToken;
    private readonly Metadata _responseTrailers;
    private readonly IDictionary<object, object> _userState;
    private Status _status;
    private WriteOptions? _writeOptions;

    private TestServerCallContext(Metadata requestHeaders, CancellationToken cancellationToken)
    {
        _requestHeaders = requestHeaders;
        _cancellationToken = cancellationToken;
        _responseTrailers = new Metadata();
        _userState = new Dictionary<object, object>();
    }

    public static TestServerCallContext Create(
        Metadata? requestHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return new TestServerCallContext(requestHeaders ?? new Metadata(), cancellationToken);
    }

    protected override string MethodCore => "TestMethod";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "127.0.0.1";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore => _requestHeaders;
    protected override CancellationToken CancellationTokenCore => _cancellationToken;
    protected override Metadata ResponseTrailersCore => _responseTrailers;
    protected override Status StatusCore { get => _status; set => _status = value; }
    protected override WriteOptions? WriteOptionsCore { get => _writeOptions; set => _writeOptions = value; }
    protected override AuthContext AuthContextCore => null!;
    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => null!;
    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    protected override IDictionary<object, object> UserStateCore => _userState;
}
