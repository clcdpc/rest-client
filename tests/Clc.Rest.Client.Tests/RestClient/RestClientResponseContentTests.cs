using System.Net;
using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientResponseContentTests
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Response_Content_Available_In_RestResponse_Response_Content()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"message\":\"ok\"}"));
        var client = CreateClient(handler);
        client.Diagnostics.CaptureResponseContent = true;

        var response = await client.ExecuteAsync<Dictionary<string, string>>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.AreEqual("{\"message\":\"ok\"}", response.Response!.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_String_Response_Uses_Raw_Body()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("plain-text"));
        var client = CreateClient(handler);
        client.Diagnostics.CaptureResponseContent = true;

        var response = await client.ExecuteAsync<string>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.AreEqual("plain-text", response.Data);
        Assert.AreEqual("plain-text", response.Response!.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_Bool_Response_Reflects_Success_Status()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ignored", Encoding.UTF8, "text/plain")
        });
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<bool>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.IsTrue(response.Data);
    }

    [TestMethod]
    public async Task ExecuteAsync_Uses_Same_Content_For_Metadata_And_Deserialization()
    {
        var payload = "{\"Name\":\"FromBody\"}";
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(payload));
        var client = CreateClient(handler);
        client.Diagnostics.CaptureResponseContent = true;

        var response = await client.ExecuteAsync<Payload>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.AreEqual(payload, response.Response!.Content);
        Assert.AreEqual("FromBody", response.Data!.Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_Response_Content_Is_Read_Only_Once()
    {
        var content = new ThrowOnSecondReadContent("{\"Name\":\"Once\"}");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = CreateClient(handler);
        client.Diagnostics.CaptureResponseContent = true;

        var response = await client.ExecuteAsync<Payload>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("Once", response.Data!.Name);
        Assert.AreEqual("{\"Name\":\"Once\"}", response.Response!.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_FormatOutputAsync_Receives_Already_Read_Content()
    {
        var content = new ThrowOnSecondReadContent("from formatter");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = CreateClient(handler);
        client.Diagnostics.CaptureResponseContent = true;
        var request = new RestRequest(HttpMethod.Get, "/data");
        string capturedContent = string.Empty;
        CancellationToken capturedToken = default;

        request.FormatOutputAsync = (_, formatterContent, cancellationToken) =>
        {
            capturedContent = formatterContent!;
            capturedToken = cancellationToken;
            return Task.FromResult<object?>($"formatted:{formatterContent}");
        };

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("formatted:from formatter", response.Data);
        Assert.AreEqual("from formatter", capturedContent);
        Assert.AreEqual(TestContext.CancellationToken, capturedToken);
        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("from formatter", response.Response!.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_Disposes_HttpResponseMessage_And_Content_After_Completion()
    {
        var content = new DisposableTrackingContent("{\"Name\":\"Disposed\"}");
        var responseMessage = new DisposableTrackingHttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content
        };
        var handler = new FakeHttpMessageHandler(_ => responseMessage);
        var client = CreateClient(handler);
        client.Diagnostics.CaptureResponseContent = true;

        var response = await client.ExecuteAsync<Payload>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.IsTrue(responseMessage.IsDisposed);
        Assert.IsTrue(content.IsDisposed);
        Assert.AreEqual("Disposed", response.Data!.Name);
        Assert.AreEqual("{\"Name\":\"Disposed\"}", response.Response!.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_Copies_Response_Headers_Before_Disposing_HttpResponseMessage()
    {
        var responseMessage = new DisposableTrackingHttpResponseMessage(HttpStatusCode.OK);
        responseMessage.Headers.Add("X-Test-Header", "response-value");
        responseMessage.Content = new DisposableTrackingContent("plain-text");
        responseMessage.Content.Headers.TryAddWithoutValidation("X-Content-Test", "content-value");

        var handler = new FakeHttpMessageHandler(_ => responseMessage);
        var client = CreateClient(handler);
        client.Diagnostics.CaptureResponseContent = true;

        var response = await client.ExecuteAsync<string>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.IsTrue(responseMessage.IsDisposed);
        Assert.AreEqual("plain-text", response.Response!.Content);
        Assert.IsTrue(response.Response.Headers.ContainsKey("X-Test-Header"));
        CollectionAssert.Contains(response.Response.Headers["X-Test-Header"], "response-value");
        Assert.IsTrue(response.Response.Headers.ContainsKey("x-test-header"));
        Assert.IsTrue(response.Response.ContentHeaders.ContainsKey("X-Content-Test"));
        CollectionAssert.Contains(response.Response.ContentHeaders["X-Content-Test"], "content-value");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ToString_Does_Not_Throw_When_Data_Or_Content_Is_Null(bool useRestResponse)
    {
        if (useRestResponse)
        {
            var restResponse = new RestResponse<string> { Data = null! };
            _ = restResponse.ToString();
            return;
        }

        var httpResponse = new HttpResponse { Content = null! };
        _ = httpResponse.ToString();
    }
}
