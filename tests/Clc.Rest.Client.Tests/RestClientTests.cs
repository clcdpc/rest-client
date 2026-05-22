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
    public async Task ExecuteAsync_Public_API_Shape_Is_Expected()
    {
        var methods = typeof(RestClient).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var executeAsync = methods.Where(m => m.Name == "ExecuteAsync").ToList();
        Assert.AreEqual(3, executeAsync.Count);
        Assert.IsFalse(methods.Any(m => m.Name is "GetAsync" or "PostAsync" or "PutAsync" or "PatchAsync" or "DeleteAsync"));

        Assert.IsTrue(executeAsync.Any(m => Matches(m, typeof(RestRequest), typeof(CancellationToken))));
        Assert.IsTrue(executeAsync.Any(m => Matches(m, typeof(string), typeof(CancellationToken))));
        Assert.IsTrue(executeAsync.Any(m => Matches(m, typeof(HttpMethod), typeof(string), typeof(CancellationToken))));
    }

    [TestMethod]
    public void Legacy_Formatter_API_Is_Removed()
    {
        var publicProtectedInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Assert.IsFalse(typeof(RestClient).GetMethods(publicProtectedInstance).Any(m => m.Name == "FormatResponse"));
        Assert.IsNull(typeof(RestClient).GetMethod("IsFormatResponseOverridden", publicProtectedInstance));
        Assert.IsNull(typeof(RestClient).GetMethod("CreateCompatibilityResponse", publicProtectedInstance));
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
    public async Task Client_Formatter_Uses_Supplied_Content()
    {
        var content = new ThrowOnSecondReadContent("expected");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = new ContentRecordingRestClient(new HttpClient(handler)) { BaseUrl = "https://example.test" };

        var response = await client.ExecuteAsync<string>("/data");

        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("expected", client.SeenContent);
        Assert.AreEqual("expected", response.Data);
        Assert.IsNull(response.Exception);
    }

    [TestMethod]
    public async Task Request_Formatter_Uses_Supplied_Content()
    {
        var content = new ThrowOnSecondReadContent("hello");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = CreateClient(handler);

        var request = new RestRequest(HttpMethod.Get, "/data")
        {
            FormatOutputAsync = (response, responseContent, token) => Task.FromResult<object>($"value:{responseContent}")
        };

        var response = await client.ExecuteAsync<string>(request);

        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("value:hello", response.Data);
        Assert.IsNull(response.Exception);
    }

    [TestMethod]
    public async Task ExecuteAsync_CancellationToken_Reaches_Handler_For_All_Public_Async_Shapes()
    {
        var cts = new CancellationTokenSource();

        var handler1 = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        await CreateClient(handler1).ExecuteAsync<string>("/data", cts.Token);
        Assert.AreEqual(cts.Token, handler1.LastCancellationToken);

        var handler2 = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        await CreateClient(handler2).ExecuteAsync<string>(HttpMethod.Post, "/data", cts.Token);
        Assert.AreEqual(cts.Token, handler2.LastCancellationToken);

        var handler3 = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var request = new RestRequest(HttpMethod.Post, "/data", new { Name = "Body" }, new Dictionary<string, string> { ["a"] = "b" });
        await CreateClient(handler3).ExecuteAsync<string>(request, cts.Token);
        Assert.AreEqual(cts.Token, handler3.LastCancellationToken);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_Cancelled_Before_Send_Captures_OperationCanceledException()
    {
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>("/data", tokenSource.Token);

        Assert.IsInstanceOfType<OperationCanceledException>(response.Exception);
    }

    private static bool Matches(MethodInfo method, params Type[] parameters)
    {
        if (!method.IsGenericMethodDefinition) return false;
        var actual = method.GetParameters().Select(p => p.ParameterType).ToArray();
        return actual.SequenceEqual(parameters);
    }

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

    private sealed class ContentRecordingRestClient(HttpClient client) : Clc.Rest.RestClient(client)
    {
        public string? SeenContent { get; private set; }

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

    private sealed class Payload { public string Name { get; set; } = string.Empty; }

    private sealed class SingleReadTrackingContent(string body) : StringContent(body, Encoding.UTF8, "application/json")
    {
        public int ReadCount { get; private set; }
        protected override Task<Stream> CreateContentReadStreamAsync() { ReadCount++; return base.CreateContentReadStreamAsync(); }
    }

    private sealed class ThrowOnSecondReadContent(string body) : StringContent(body, Encoding.UTF8, "text/plain")
    {
        public int ReadCount { get; private set; }
        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            ReadCount++;
            if (ReadCount > 1) throw new InvalidOperationException("Content read more than once.");
            return base.CreateContentReadStreamAsync();
        }
    }
}
