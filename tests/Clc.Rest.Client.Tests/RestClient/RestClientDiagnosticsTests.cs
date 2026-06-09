using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientDiagnosticsTests
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task ExecuteAsync_Serialized_Request_BodyString_Is_Captured_By_Default()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var expectedBody = "{\"Name\":\"Alice\"}";

        var response = await client.ExecuteAsync<string>(RestRequest.Post("/post", new { Name = "Alice" }), TestContext.CancellationToken);
        var sentBody = await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual(expectedBody, response.BodyString);
        Assert.AreEqual(expectedBody, sentBody);
    }

    [TestMethod]
    public async Task ExecuteAsync_Explicit_HttpContent_Is_Not_Captured_By_Default()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = RestRequest.WithContent(
            HttpMethod.Post,
            "/token",
            new StringContent("secret=value", Encoding.UTF8, "text/plain"));

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);
        var sentBody = await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.IsNull(response.BodyString);
        Assert.AreEqual("secret=value", sentBody);
    }

    [TestMethod]
    public async Task ExecuteAsync_Response_Content_Is_Captured_By_Default()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"Name\":\"Alice\"}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Payload>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.IsNotNull(response.Response);
        Assert.AreEqual("{\"Name\":\"Alice\"}", response.Response.Content);
        Assert.AreEqual("Alice", response.Data!.Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_Custom_Content_From_AddBody_Override_Is_Captured_Only_When_Explicit_Capture_Is_Enabled()
    {
        var defaultClient = new CustomBodyRestClient { BaseUrl = "https://example.test" };

        var defaultResponse = await defaultClient.ExecuteAsync<string>(RestRequest.Post("/post"), TestContext.CancellationToken);
        var defaultSentBody = await defaultClient.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.IsNull(defaultResponse.Exception);
        Assert.IsNull(defaultResponse.BodyString);
        Assert.AreEqual("custom-content", defaultSentBody);

        var enabledClient = new CustomBodyRestClient { BaseUrl = "https://example.test" };
        enabledClient.Diagnostics.CaptureExplicitRequestContent = true;

        var enabledResponse = await enabledClient.ExecuteAsync<string>(RestRequest.Post("/post"), TestContext.CancellationToken);

        Assert.IsNull(enabledResponse.Exception);
        Assert.AreEqual("custom-content", enabledResponse.BodyString);
    }

    [TestMethod]
    public async Task ExecuteAsync_Explicit_HttpContent_Is_Captured_When_Enabled()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        client.Diagnostics.CaptureExplicitRequestContent = true;
        var request = RestRequest.WithContent(
            HttpMethod.Post,
            "/token",
            new StringContent("secret=value", Encoding.UTF8, "text/plain"));

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("secret=value", response.BodyString);
    }

    [TestMethod]
    public async Task ExecuteAsync_Serialized_Request_Body_Capture_Can_Be_Disabled()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        client.Diagnostics.CaptureSerializedRequestBody = false;
        var expectedBody = "{\"Name\":\"Alice\"}";

        var response = await client.ExecuteAsync<string>(RestRequest.Post("/post", new { Name = "Alice" }), TestContext.CancellationToken);
        var sentBody = await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.IsNull(response.BodyString);
        Assert.AreEqual(expectedBody, sentBody);
    }

    [TestMethod]
    public async Task ExecuteAsync_Response_Content_Capture_Can_Be_Disabled_Without_Breaking_Deserialization()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("abcdefghijklmnopqrstuvwxyz"));
        var client = CreateClient(handler);
        client.Diagnostics.CaptureResponseContent = false;

        var response = await client.ExecuteAsync<string>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.IsNotNull(response.Response);
        Assert.IsNull(response.Response.Content);
        Assert.AreEqual("abcdefghijklmnopqrstuvwxyz", response.Data);
    }

    [TestMethod]
    public async Task ExecuteAsync_MaxCapturedContentLength_Truncates_Request_BodyString()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        client.Diagnostics.MaxCapturedContentLength = 5;
        var expectedBody = "{\"Name\":\"Alice\"}";

        var response = await client.ExecuteAsync<string>(RestRequest.Post("/post", new { Name = "Alice" }), TestContext.CancellationToken);
        var sentBody = await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual(5, response.BodyString!.Length);
        Assert.AreEqual(expectedBody[..5], response.BodyString);
        Assert.AreEqual(expectedBody, sentBody);
    }

    [TestMethod]
    public async Task ExecuteAsync_MaxCapturedContentLength_Truncates_Stored_Response_Content_But_Not_Data()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("abcdefghijklmnopqrstuvwxyz"));
        var client = CreateClient(handler);
        client.Diagnostics.MaxCapturedContentLength = 5;

        var response = await client.ExecuteAsync<string>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("abcde", response.Response!.Content);
        Assert.AreEqual("abcdefghijklmnopqrstuvwxyz", response.Data);
    }

    [TestMethod]
    public async Task ExecuteAsync_MaxCapturedContentLength_Zero_Captures_Empty_Strings_When_Content_Exists()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("abcdefghijklmnopqrstuvwxyz"));
        var client = CreateClient(handler);
        client.Diagnostics.MaxCapturedContentLength = 0;

        var response = await client.ExecuteAsync<string>(RestRequest.Post("/post", new { Name = "Alice" }), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual(string.Empty, response.BodyString);
        Assert.AreEqual(string.Empty, response.Response!.Content);
        Assert.AreEqual("abcdefghijklmnopqrstuvwxyz", response.Data);
    }

    [TestMethod]
    public async Task ExecuteAsync_Negative_MaxCapturedContentLength_Captures_Exception()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        client.Diagnostics.MaxCapturedContentLength = -1;

        var response = await client.ExecuteAsync<string>(RestRequest.Post("/post", new { Name = "Alice" }), TestContext.CancellationToken);

        Assert.IsNotNull(response.Exception);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(response.Exception);
        StringAssert.Contains(response.Exception.Message, "MaxCapturedContentLength");
    }

    [TestMethod]
    public async Task ExecuteAsync_Negative_MaxCapturedContentLength_Captures_Exception_When_Response_Content_Is_Captured()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("response-body"));
        var client = CreateClient(handler);
        client.Diagnostics.MaxCapturedContentLength = -1;

        var response = await client.ExecuteAsync<string>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.IsNotNull(handler.LastRequest);
        Assert.IsNotNull(response.Exception);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(response.Exception);
        StringAssert.Contains(response.Exception.Message, "MaxCapturedContentLength");
        Assert.IsNull(response.Data);
    }
}
