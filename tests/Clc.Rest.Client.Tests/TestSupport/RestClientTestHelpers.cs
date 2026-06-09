using System.Net;
using System.Net.Http;
using System.Text;

namespace Clc.Rest.Client.Tests;

internal static class RestClientTestHelpers
{
    public static TestRestClient CreateClient(HttpMessageHandler handler)
        => new(new HttpClient(handler)) { BaseUrl = "https://example.test" };

    public static HttpResponseMessage JsonResponse(string content)
        => new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };
}
