using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

internal sealed class CustomBodyRestClient : Clc.Rest.RestClient
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override void AddBody(RestRequest request, HttpRequestMessage httpRequest)
    {
        httpRequest.Content = new StringContent("custom-content", Encoding.UTF8, "text/plain");
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(JsonResponse("{}"));
    }
}
