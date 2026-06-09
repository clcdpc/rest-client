using System.Net.Http;

namespace Clc.Rest.Client.Tests;

internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _callback = callback;
    public HttpRequestMessage? LastRequest { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(_callback(request));
    }
}
