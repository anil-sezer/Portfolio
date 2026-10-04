using System.Net;

namespace Portfolio.Test.Shared;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public List<HttpRequestMessage> SentRequests { get; } = [];

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = (req, _) => Task.FromResult(handler(req));
    }

    public MockHttpMessageHandler(HttpStatusCode statusCode, HttpContent? content = null)
    {
        _handler = (_, _) => Task.FromResult(new HttpResponseMessage(statusCode) { Content = content ?? new StringContent(string.Empty) });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        return await _handler(request, cancellationToken);
    }
}
