using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientTests
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Post_With_Body_Preserves_Serialized_Body()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/post", new { Name = "Alice" }));

        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.AreEqual("{\"Name\":\"Alice\"}", await handler.LastRequest.Content!.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public void Public_Async_Api_Shape_Is_Restricted()
    {
        var methods = typeof(RestClient).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        Assert.AreEqual(3, methods.Count(m => m.Name == "ExecuteAsync"));

        Assert.IsFalse(methods.Any(m => m.Name is "GetAsync" or "PostAsync" or "PutAsync" or "PatchAsync" or "DeleteAsync"));

        Assert.IsTrue(methods.Any(IsExecuteAsyncSignature(typeof(RestRequest), typeof(CancellationToken))));
        Assert.IsTrue(methods.Any(IsExecuteAsyncSignature(typeof(string), typeof(CancellationToken))));
        Assert.IsTrue(methods.Any(IsExecuteAsyncSignature(typeof(HttpMethod), typeof(string), typeof(CancellationToken))));
    }

    [TestMethod]
    public void Legacy_Formatter_Api_Is_Removed()
    {
        var methods = typeof(RestClient).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsFalse(methods.Any(m => (m.IsPublic || m.IsFamily) && m.Name == "FormatResponse"));
        Assert.IsFalse(methods.Any(m => m.Name == "IsFormatResponseOverridden"));
        Assert.IsFalse(methods.Any(m => m.Name == "CreateCompatibilityResponse"));
    }

    [TestMethod]
    public async Task ExecuteAsync_Response_Content_Is_Read_Only_Once()
    {
        var content = new SingleReadTrackingContent("{\"Name\":\"Once\"}");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Payload>("/data");

        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("Once", response.Data.Name);
        Assert.AreEqual("{\"Name\":\"Once\"}", response.Response.Content);
        Assert.IsNull(response.Exception);
    }

    [TestMethod]
    public async Task Client_Custom_Formatter_Uses_Supplied_Content()
    {
        var content = new ThrowOnSecondReadContent("formatted");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = new ContentCapturingClient(new HttpClient(handler)) { BaseUrl = "https://example.test" };

        var response = await client.ExecuteAsync<string>("/data");

        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("formatted", client.SeenContent);
        Assert.AreEqual("formatted", response.Data);
        Assert.IsNull(response.Exception);
    }

    [TestMethod]
    public async Task Request_Formatter_Uses_Supplied_Content()
    {
        var content = new ThrowOnSecondReadContent("value");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = CreateClient(handler);

        var request = new RestRequest(HttpMethod.Get, "/data")
        {
            FormatOutputAsync = (response, responseContent, cancellationToken) => Task.FromResult<object>($"x-{responseContent}")
        };

        var response = await client.ExecuteAsync<string>(request);

        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("x-value", response.Data);
        Assert.IsNull(response.Exception);
    }

    [TestMethod]
    public async Task ExecuteAsync_Passes_CancellationToken_All_Supported_Shapes()
    {
        var tokenSource = new CancellationTokenSource();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>("/data", tokenSource.Token);
        Assert.AreEqual(tokenSource.Token, handler.LastCancellationToken);

        await client.ExecuteAsync<string>(HttpMethod.Post, "/data", tokenSource.Token);
        Assert.AreEqual(tokenSource.Token, handler.LastCancellationToken);

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/data", new { Name = "Body" }, new Dictionary<string, string> { ["a"] = "b" }), tokenSource.Token);
        Assert.AreEqual(tokenSource.Token, handler.LastCancellationToken);
    }

    private static Func<MethodInfo, bool> IsExecuteAsyncSignature(params Type[] parameterTypes) => m =>
        m.Name == "ExecuteAsync" &&
        m.IsGenericMethod &&
        m.GetGenericArguments().Length == 1 &&
        m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes);

    private static TestRestClient CreateClient(HttpMessageHandler handler)
        => new(new HttpClient(handler)) { BaseUrl = "https://example.test" };

    private static HttpResponseMessage JsonResponse(string content)
        => new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
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

    private sealed class TestRestClient(HttpClient client) : Clc.Rest.RestClient(client);

    private sealed class ContentCapturingClient(HttpClient client) : Clc.Rest.RestClient(client)
    {
        public string SeenContent { get; private set; } = string.Empty;

        public override Task<T> FormatResponseAsync<T>(HttpResponseMessage response, string content, CancellationToken cancellationToken = default)
        {
            SeenContent = content;
            if (typeof(T) == typeof(string))
            {
                return Task.FromResult((T)(object)content);
            }

            return base.FormatResponseAsync<T>(response, content, cancellationToken);
        }
    }

    private sealed class Payload
    {
        public string Name { get; set; } = string.Empty;
    }

    private class SingleReadTrackingContent(string body) : StringContent(body, Encoding.UTF8, "application/json")
    {
        public int ReadCount { get; private set; }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            ReadCount++;
            return base.CreateContentReadStreamAsync();
        }
    }

    private sealed class ThrowOnSecondReadContent(string body) : SingleReadTrackingContent(body)
    {
        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            if (ReadCount >= 1)
            {
                throw new InvalidOperationException("content read more than once");
            }

            return base.CreateContentReadStreamAsync();
        }
    }
}
