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
        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/post", body: new { Name = "Alice" }), TestContext.CancellationToken);
        Assert.AreEqual("{\"Name\":\"Alice\"}", await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ExecuteAsync_Request_BodyString_Is_Captured()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var response = await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/post", body: new { Name = "Alice" }), TestContext.CancellationToken);
        Assert.AreEqual("{\"Name\":\"Alice\"}", response.BodyString);
    }

    [TestMethod]
    public async Task ExecuteAsync_Response_Content_Is_Read_Only_Once()
    {
        var content = new ThrowOnSecondReadContent("{\"Name\":\"Once\"}");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = CreateClient(handler);
        var response = await client.ExecuteAsync<Payload>("/data", TestContext.CancellationToken);
        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("Once", response.Data.Name);
    }

    [TestMethod]
    public void Public_Async_Api_Shape_Is_Simplified_And_Legacy_Free()
    {
        var methods = typeof(Clc.Rest.RestClient).GetMethods(BindingFlags.Instance | BindingFlags.Public);
        var executeAsync = methods.Where(m => m.Name == "ExecuteAsync" && m.IsGenericMethodDefinition).ToList();
        Assert.AreEqual(3, executeAsync.Count);
        Assert.IsNotNull(executeAsync.SingleOrDefault(m => SignatureMatches(m, typeof(RestRequest), typeof(CancellationToken))));
        Assert.IsNotNull(executeAsync.SingleOrDefault(m => SignatureMatches(m, typeof(string), typeof(CancellationToken))));
        Assert.IsNotNull(executeAsync.SingleOrDefault(m => SignatureMatches(m, typeof(HttpMethod), typeof(string), typeof(CancellationToken))));
        Assert.IsFalse(methods.Any(m => m.Name is "GetAsync" or "PostAsync" or "PutAsync" or "PatchAsync" or "DeleteAsync"));
        Assert.IsFalse(methods.Any(m => m.Name == "FormatResponse"));
        Assert.IsFalse(methods.Any(m => m.Name == "IsFormatResponseOverridden"));
        Assert.IsFalse(methods.Any(m => m.Name == "CreateCompatibilityResponse"));
    }

    private static bool SignatureMatches(MethodInfo method, params Type[] types)
        => method.GetParameters().Select(p => p.ParameterType).SequenceEqual(types);

    [TestMethod]
    public async Task ExecuteAsync_Passes_CancellationToken_To_HttpMessageHandler()
    {
        var tokenSource = new CancellationTokenSource();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        await client.ExecuteAsync<string>("/data", tokenSource.Token);
        Assert.AreEqual(tokenSource.Token, handler.LastCancellationToken);
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

    private sealed class Payload
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ThrowOnSecondReadContent(string body) : StringContent(body, Encoding.UTF8, "application/json")
    {
        public int ReadCount { get; private set; }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            ReadCount++;
            if (ReadCount > 1)
            {
                throw new InvalidOperationException("Content was read more than once.");
            }

            return base.CreateContentReadStreamAsync();
        }
    }
}
