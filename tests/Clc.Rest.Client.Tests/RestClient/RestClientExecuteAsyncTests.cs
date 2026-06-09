using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientExecuteAsyncTests
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task InjectedHttpClient_IsUsedForRequests()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"value\":true}"));
        using var httpClient = new HttpClient(handler);
        var client = new TestRestClient(httpClient)
        {
            BaseUrl = "https://example.test"
        };

        var response = await client.ExecuteAsync<string>(RestRequest.Get("/test"), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.IsNotNull(handler.LastRequest);
        Assert.AreEqual("https://example.test/test", handler.LastRequest.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task ExecuteAsync_DoesNotMutateDefaultRequestHeaders()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"ok\":true}"));
        using var httpClient = new HttpClient(handler);
        var client = new TestRestClient(httpClient)
        {
            BaseUrl = "https://example.test"
        };
        var request = RestRequest.Get("/test");
        request.Headers["X-Test"] = "abc";

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.IsFalse(httpClient.DefaultRequestHeaders.Contains("X-Test"));
        Assert.IsNotNull(handler.LastRequest);
        Assert.IsTrue(handler.LastRequest.Headers.Contains("X-Test"));
    }

    [TestMethod]
    public async Task ExecuteAsync_Uses_SendAsync_Hook_For_Transport_Extension()
    {
        using var tokenSource = new CancellationTokenSource();
        var client = new SendHookRestClient
        {
            BaseUrl = "https://example.test"
        };

        var response = await client.ExecuteAsync<Dictionary<string, bool>>(RestRequest.Get("/test"), tokenSource.Token);

        Assert.IsNull(response.Exception);
        Assert.IsNotNull(client.LastRequest);
        Assert.AreEqual("https://example.test/test", client.LastRequest.RequestUri!.AbsoluteUri);
        Assert.AreEqual(tokenSource.Token, client.LastCancellationToken);
    }

    [TestMethod]
    public async Task ExecuteAsync_Uses_CreateHttpRequestMessage_Hook_For_Message_Customization()
    {
        var client = new CustomRequestMessageRestClient
        {
            BaseUrl = "https://example.test"
        };
        var request = new RestRequest(HttpMethod.Post, "/test", body: new { Name = "Alice" }, queryParameters: new Dictionary<string, object>
        {
            ["q"] = "value"
        });
        request.Headers["X-Test"] = "abc";

        var response = await client.ExecuteAsync<Dictionary<string, bool>>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.IsNotNull(client.LastRequest);
        Assert.AreEqual("CreateHttpRequestMessage", client.LastRequest.Headers.GetValues("X-Created-By").Single());
        Assert.AreEqual("abc", client.LastRequest.Headers.GetValues("X-Test").Single());
        Assert.AreEqual("https://example.test/test?q=value", client.LastRequest.RequestUri!.AbsoluteUri);
        Assert.AreEqual("{\"Name\":\"Alice\"}", await client.LastRequest.Content!.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ExecuteAsync_Default_Request_Headers_Do_Not_Leak_Between_Requests()
    {
        var client = new CapturingRestClient
        {
            BaseUrl = "https://example.test"
        };
        var firstRequest = RestRequest.Get("/first");
        firstRequest.Headers["X-Test"] = "one";

        var firstResponse = await client.ExecuteAsync<Dictionary<string, bool>>(firstRequest, TestContext.CancellationToken);
        var secondResponse = await client.ExecuteAsync<Dictionary<string, bool>>(RestRequest.Get("/second"), TestContext.CancellationToken);

        Assert.IsNull(firstResponse.Exception);
        Assert.IsNull(secondResponse.Exception);
        Assert.AreEqual(2, client.Requests.Count);
        Assert.AreEqual("one", client.Requests[0].Headers.GetValues("X-Test").Single());
        Assert.IsFalse(client.Requests[1].Headers.Contains("X-Test"));
    }

    [TestMethod]
    public async Task ExecuteAsync_Default_RestRequest_Does_Not_Throw_NullReferenceException()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>(new RestRequest(), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("https://example.test/", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task ExecuteAsync_Null_Path_Is_Captured_As_Predictable_Response_Exception_Or_Normalized()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Get, null!), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("https://example.test/", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task IRestClient_ExecuteAsync_Can_Call_Concrete_RestClient()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("expected-value", Encoding.UTF8, "text/plain")
        });
        IRestClient client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("expected-value", response.Data);
    }
}
