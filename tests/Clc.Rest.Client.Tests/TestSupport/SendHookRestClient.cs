using System.Net.Http;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

internal sealed class SendHookRestClient : Clc.Rest.RestClient
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(JsonResponse("{\"ok\":true}"));
    }
}
