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
    public void Public_Async_Api_Shape_Is_Restricted()
    {
        var publicInstanceMethods = typeof(Clc.Rest.RestClient).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var executeAsyncMethods = publicInstanceMethods.Where(m => m.Name == "ExecuteAsync").ToList();

        Assert.AreEqual(3, executeAsyncMethods.Count);
        Assert.IsFalse(publicInstanceMethods.Any(m => m.Name is "GetAsync" or "PostAsync" or "PutAsync" or "PatchAsync" or "DeleteAsync"));

        Assert.IsTrue(executeAsyncMethods.Any(IsExecuteAsyncRestRequestSignature));
        Assert.IsTrue(executeAsyncMethods.Any(IsExecuteAsyncStringSignature));
        Assert.IsTrue(executeAsyncMethods.Any(IsExecuteAsyncMethodUrlSignature));
    }

    [TestMethod]
    public void Legacy_Formatter_Apis_Are_Removed()
    {
        var publicProtectedInstance = typeof(Clc.Rest.RestClient).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(method => method.IsPublic || method.IsFamily)
            .ToList();

        Assert.IsFalse(publicProtectedInstance.Any(m => m.Name == "FormatResponse"));
        Assert.IsNull(typeof(Clc.Rest.RestClient).GetMethod("IsFormatResponseOverridden", BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.IsNull(typeof(Clc.Rest.RestClient).GetMethod("CreateCompatibilityResponse", BindingFlags.NonPublic | BindingFlags.Instance));
    }

    [TestMethod]
    public async Task ExecuteAsync_Normal_Response_Content_Is_Read_Exactly_Once()
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
    public async Task ExecuteAsync_Custom_Client_Formatter_Uses_Supplied_Content()
    {
        var content = new ThrowOnSecondReadContent("formatter-content");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = new RecordingFormatterClient(new HttpClient(handler)) { BaseUrl = "https://example.test" };

        var response = await client.ExecuteAsync<string>("/data");

        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("formatter-content", client.LastFormattedContent);
        Assert.AreEqual("formatter-content", response.Data);
        Assert.IsNull(response.Exception);
    }

    [TestMethod]
    public async Task ExecuteAsync_Request_Formatter_Uses_Supplied_Content()
    {
        var content = new ThrowOnSecondReadContent("request-formatter");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = CreateClient(handler);

        var request = new RestRequest(HttpMethod.Get, "/data")
        {
            FormatOutputAsync = (response, responseContent, cancellationToken) =>
                Task.FromResult<object>($"formatted-{responseContent}")
        };

        var response = await client.ExecuteAsync<string>(request);

        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("formatted-request-formatter", response.Data);
        Assert.IsNull(response.Exception);
    }

    [TestMethod]
    public async Task ExecuteAsync_Overloads_Propagate_CancellationToken()
    {
        var token = new CancellationTokenSource().Token;
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>("/data", token);
        Assert.AreEqual(token, handler.LastCancellationToken);

        await client.ExecuteAsync<string>(HttpMethod.Post, "/data", token);
        Assert.AreEqual(token, handler.LastCancellationToken);

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/data", new { Name = "Alice" }, new Dictionary<string, string> { ["a"] = "b" }), token);
        Assert.AreEqual(token, handler.LastCancellationToken);
    }

    [TestMethod]
    public async Task Post_With_Body_And_Parameters_Does_Not_Overwrite_Body()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/post", new { Id = 42 }, new Dictionary<string, string> { ["a"] = "b" }));

        var sentBody = await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.Contains("\"Id\":42", sentBody);
        Assert.DoesNotContain("a=b", sentBody);
    }

    private static bool IsExecuteAsyncRestRequestSignature(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return method.IsGenericMethodDefinition
               && parameters.Length == 2
               && parameters[0].ParameterType == typeof(RestRequest)
               && parameters[1].ParameterType == typeof(CancellationToken);
    }

    private static bool IsExecuteAsyncStringSignature(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return method.IsGenericMethodDefinition
               && parameters.Length == 2
               && parameters[0].ParameterType == typeof(string)
               && parameters[1].ParameterType == typeof(CancellationToken);
    }

    private static bool IsExecuteAsyncMethodUrlSignature(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return method.IsGenericMethodDefinition
               && parameters.Length == 3
               && parameters[0].ParameterType == typeof(HttpMethod)
               && parameters[1].ParameterType == typeof(string)
               && parameters[2].ParameterType == typeof(CancellationToken);
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

    private sealed class TestRestClient(HttpClient client) : Clc.Rest.RestClient(client)
    {
    }

    private sealed class RecordingFormatterClient(HttpClient client) : Clc.Rest.RestClient(client)
    {
        public string LastFormattedContent { get; private set; } = string.Empty;

        public override Task<T> FormatResponseAsync<T>(HttpResponseMessage response, string content, CancellationToken cancellationToken = default)
        {
            LastFormattedContent = content;
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

    private sealed class SingleReadTrackingContent(string body) : StringContent(body, Encoding.UTF8, "application/json")
    {
        public int ReadCount { get; private set; }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            ReadCount++;
            return base.CreateContentReadStreamAsync();
        }
    }

    private sealed class ThrowOnSecondReadContent(string body) : StringContent(body, Encoding.UTF8, "text/plain")
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
