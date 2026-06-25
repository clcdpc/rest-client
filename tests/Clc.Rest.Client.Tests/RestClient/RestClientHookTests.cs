using Clc.Rest.Models;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientHookTests
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public void PreformatRestRequest_DefaultBehavior_ReturnsPassedRequest()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = new RestRequest();

        var preformattedRequest = client.PreformatRestRequest(request);

        Assert.AreSame(request, preformattedRequest);
    }

    [TestMethod]
    public void PreDeserialize_Returns_Input_String_Unchanged()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var input = "{\"key\":\"value\"}";
        var result = client.PreDeserialize(input);

        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public async Task FormatResponseAsync_Applies_PreDeserialize_To_Content()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"Name\":\"old\"}"));
        var client = new PreDeserializeTestRestClient(new HttpClient(handler)) { BaseUrl = "https://example.test" };

        var response = await client.ExecuteAsync<Payload>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.AreEqual("new", response.Data!.Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_Applies_PreformatRestRequest_To_Request()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = new PreformatRestRequestTestRestClient(new HttpClient(handler)) { BaseUrl = "https://example.test" };

        var request = RestRequest.Get("/data");
        await client.ExecuteAsync<Payload>(request, TestContext.CancellationToken);

        Assert.IsNotNull(handler.LastRequest);
        Assert.AreEqual("/data-modified", handler.LastRequest.RequestUri?.AbsolutePath);
        Assert.IsTrue(handler.LastRequest.Headers.Contains("X-Custom-Header"));
    }

    private sealed class PreDeserializeTestRestClient(HttpClient client) : Clc.Rest.RestClient(client)
    {
        public override string PreDeserialize(string responseBody)
        {
            return responseBody.Replace("old", "new");
        }
    }

    private sealed class PreformatRestRequestTestRestClient(HttpClient client) : Clc.Rest.RestClient(client)
    {
        public override RestRequest PreformatRestRequest(RestRequest request)
        {
            request.Path = request.Path + "-modified";
            request.Headers["X-Custom-Header"] = "TestValue";
            return request;
        }
    }
}