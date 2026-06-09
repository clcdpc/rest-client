using System.Net.Http;
using Clc.Rest.Models;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

internal sealed class CustomRequestMessageRestClient : Clc.Rest.RestClient
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override HttpRequestMessage CreateHttpRequestMessage(RestRequest request)
    {
        var httpRequest = base.CreateHttpRequestMessage(request);
        httpRequest.Headers.Add("X-Created-By", "CreateHttpRequestMessage");
        return httpRequest;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(JsonResponse("{\"ok\":true}"));
    }
}
