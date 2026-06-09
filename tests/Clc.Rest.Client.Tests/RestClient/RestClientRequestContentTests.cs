using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientRequestContentTests
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Post_With_Body_Preserves_Serialized_Body()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/post", body: new { Name = "Alice" }), TestContext.CancellationToken);

        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.AreEqual("{\"Name\":\"Alice\"}", await handler.LastRequest.Content!.ReadAsStringAsync(TestContext.CancellationToken));
        Assert.AreEqual("application/json; charset=utf-8", handler.LastRequest.Content.Headers.ContentType!.ToString());
    }

    [TestMethod]
    public async Task ExecuteAsync_Uses_PostForm_Factory_As_FormUrlEncodedContent()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(RestRequest.PostForm("/post", new Dictionary<string, string>
        {
            ["first"] = "one",
            ["second"] = "two"
        }), TestContext.CancellationToken);

        Assert.AreEqual("application/x-www-form-urlencoded", handler.LastRequest!.Content!.Headers.ContentType!.MediaType);
        var payload = await handler.LastRequest.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.Contains("first=one", payload);
        Assert.Contains("second=two", payload);
    }

    [TestMethod]
    public async Task ExecuteAsync_Uses_Post_Factory_Request_With_Body_And_QueryParameters()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(RestRequest.Post("/post", new { Id = 42 }, new Dictionary<string, object> { ["a"] = "b" }), TestContext.CancellationToken);

        var sentBody = await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.Contains("\"Id\":42", sentBody);
        Assert.DoesNotContain("a=b", sentBody);
        Assert.AreEqual("application/json; charset=utf-8", handler.LastRequest.Content.Headers.ContentType!.ToString());
    }

    [TestMethod]
    public async Task ExecuteAsync_Content_Takes_Precedence_Over_Body()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = RestRequest.Post("/post", new { Name = "body" });
        request.Content = new StringContent("raw-content", Encoding.UTF8, "text/plain");

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("raw-content", await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken));
        Assert.AreEqual("text/plain", handler.LastRequest.Content.Headers.ContentType!.MediaType);
    }

    [TestMethod]
    public async Task ExecuteAsync_Authenticator_Sees_Final_Uri_Headers_And_Content()
    {
        var authenticator = new InspectingAuthenticator();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var request = RestRequest.Post(
            "/items?existing=1",
            new { Name = "Alice" },
            new Dictionary<string, object> { ["q"] = "value" });

        request.Headers["X-Test"] = "abc";
        request.Authenticator = authenticator;

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("https://example.test/items?existing=1&q=value", authenticator.Uri);
        Assert.AreEqual("abc", authenticator.HeaderValue);
        Assert.IsTrue(authenticator.SawContent);
        Assert.AreEqual("application/json; charset=utf-8", authenticator.ContentType);
        Assert.AreEqual("{\"Name\":\"Alice\"}", await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken));
    }
}
