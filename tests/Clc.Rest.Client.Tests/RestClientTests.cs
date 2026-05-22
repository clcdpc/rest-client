using System.Net;
using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        Assert.AreEqual("application/json; charset=utf-8", handler.LastRequest.Content.Headers.ContentType!.ToString());
    }

    [TestMethod]
    public async Task Post_With_Parameters_And_No_Body_Uses_FormUrlEncodedContent()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/post", parameters: new Dictionary<string, string>
        {
            ["first"] = "one",
            ["second"] = "two"
        }));

        Assert.AreEqual("application/x-www-form-urlencoded", handler.LastRequest!.Content!.Headers.ContentType!.MediaType);
        var payload = await handler.LastRequest.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.Contains("first=one", payload);
        Assert.Contains("second=two", payload);
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
        Assert.AreEqual("application/json; charset=utf-8", handler.LastRequest.Content.Headers.ContentType!.ToString());
    }

    [TestMethod]
    public async Task Get_With_Parameters_Appends_Query_String()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Get, "/search", parameters: new Dictionary<string, string>
        {
            ["q"] = "value",
            ["n"] = "10"
        }));

        var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.StartsWith("https://example.test/search?", uri);
        Assert.Contains("q=value", uri);
        Assert.Contains("n=10", uri);
    }

    [TestMethod]
    [DataRow("GET")]
    [DataRow("PUT")]
    [DataRow("PATCH")]
    [DataRow("DELETE")]
    public async Task NonPost_Methods_With_Parameters_Append_Query_String(string method)
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(new RestRequest(new HttpMethod(method), "/resource", parameters: new Dictionary<string, string>
        {
            ["x y"] = "a&b",
            ["p"] = "q"
        }));

        var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.Contains("x%20y=a%26b", uri);
        Assert.Contains("p=q", uri);
    }

    [TestMethod]
    public async Task Query_String_Appending_Preserves_Existing_Query_And_Uses_Separators_Correctly()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Get, "/search?existing=1", parameters: new Dictionary<string, string>
        {
            ["new key"] = "new value"
        }));

        var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.StartsWith("https://example.test/search?existing=1&", uri);
        Assert.Contains("new%20key=new%20value", uri);
    }


    [TestMethod]
    public async Task NonPost_With_Fragment_Appends_Query_Before_Fragment()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Put, "/resource#frag", parameters: new Dictionary<string, string>
        {
            ["x"] = "1"
        }));

        Assert.AreEqual("https://example.test/resource?x=1#frag", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task NonPost_With_Relative_Uri_Uses_BaseAddress_And_Appends_Query()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var client = new TestRestClient(httpClient) { BaseUrl = string.Empty };

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Delete, "relative/path", parameters: new Dictionary<string, string>
        {
            ["x"] = "1"
        }));

        Assert.AreEqual("https://example.test/relative/path?x=1", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task Response_Content_Available_In_RestResponse_Response_Content()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"message\":\"ok\"}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Dictionary<string, string>>("/data");

        Assert.AreEqual("{\"message\":\"ok\"}", response.Response.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_String_Response_Uses_Raw_Body()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("plain-text"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>("/data");

        Assert.AreEqual("plain-text", response.Data);
        Assert.AreEqual("plain-text", response.Response.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_Bool_Response_Reflects_Success_Status()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ignored", Encoding.UTF8, "text/plain")
        });
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<bool>("/data");

        Assert.IsTrue(response.Data);
    }

    [TestMethod]
    public async Task ExecuteAsync_Uses_Same_Content_For_Metadata_And_Deserialization()
    {
        var payload = "{\"Name\":\"FromBody\"}";
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(payload));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Payload>("/data");

        Assert.AreEqual(payload, response.Response.Content);
        Assert.AreEqual("FromBody", response.Data.Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_Request_BodyString_Is_Captured()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/post", new { Name = "Alice" }));

        Assert.AreEqual("{\"Name\":\"Alice\"}", response.BodyString);
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
    }

    [TestMethod]
    public async Task ExecuteAsync_Passes_CancellationToken_To_HttpMessageHandler()
    {
        var tokenSource = new CancellationTokenSource();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>("/data", cancellationToken: tokenSource.Token);

        Assert.AreEqual(tokenSource.Token, handler.LastCancellationToken);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_Cancelled_Before_Send_Captures_OperationCanceledException()
    {
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>("/data", cancellationToken: tokenSource.Token);

        Assert.IsInstanceOfType<OperationCanceledException>(response.Exception);
        Assert.IsNull(handler.LastRequest);
    }

    [TestMethod]
    public async Task ExecuteAsync_With_Body_When_Cancelled_Before_Send_Captures_OperationCanceledException()
    {
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/data", new { Name = "Body" }), tokenSource.Token);

        Assert.IsInstanceOfType<OperationCanceledException>(response.Exception);
        Assert.IsNull(handler.LastRequest);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_SendAsync_Throws_HttpRequestException_Captures_Exception()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("network"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>("/data");

        Assert.IsInstanceOfType<HttpRequestException>(response.Exception);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_Deserialization_Fails_Captures_Exception()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{not-json"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Payload>("/data");

        Assert.IsNotNull(response.Exception);
    }

    [TestMethod]
    public void Public_Async_Api_Shape_Is_Restricted()
    {
        var methods = typeof(RestClient).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        var executeAsyncMethods = methods.Where(m => m.Name == "ExecuteAsync").ToArray();
        Assert.AreEqual(3, executeAsyncMethods.Length);
        Assert.IsFalse(methods.Any(m => m.Name is "GetAsync" or "PostAsync" or "PutAsync" or "PatchAsync" or "DeleteAsync"));

        Assert.IsTrue(executeAsyncMethods.Any(m => MatchesSignature(m, typeof(RestRequest), typeof(CancellationToken))));
        Assert.IsTrue(executeAsyncMethods.Any(m => MatchesSignature(m, typeof(string), typeof(CancellationToken))));
        Assert.IsTrue(executeAsyncMethods.Any(m => MatchesSignature(m, typeof(HttpMethod), typeof(string), typeof(CancellationToken))));
    }

    [TestMethod]
    public void Legacy_Formatter_Apis_Are_Removed()
    {
        var allMethods = typeof(RestClient).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsFalse(allMethods.Any(m => m.Name == "FormatResponse"));
        Assert.IsFalse(allMethods.Any(m => m.Name == "IsFormatResponseOverridden"));
        Assert.IsFalse(allMethods.Any(m => m.Name == "CreateCompatibilityResponse"));
    }

    [TestMethod]
    public async Task Custom_Client_Formatter_Uses_Supplied_Content()
    {
        var content = new ThrowOnSecondReadContent("formatter-body");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = new RecordingFormatRestClient(new HttpClient(handler)) { BaseUrl = "https://example.test" };

        var response = await client.ExecuteAsync<string>("/data");

        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("formatter-body", client.LastContent);
        Assert.AreEqual("formatter-body", response.Data);
        Assert.IsNull(response.Exception);
    }

    [TestMethod]
    public async Task Request_Specific_Formatter_Uses_Supplied_Content()
    {
        var content = new ThrowOnSecondReadContent("value");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = CreateClient(handler);
        var request = new RestRequest(HttpMethod.Get, "/data")
        {
            FormatOutputAsync = (response, responseContent, cancellationToken) => Task.FromResult<object>($"formatted:{responseContent}")
        };

        var result = await client.ExecuteAsync<string>(request);

        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("formatted:value", result.Data);
        Assert.IsNull(result.Exception);
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

    private static bool MatchesSignature(MethodInfo method, params Type[] parameterTypes)
    {
        var parameters = method.GetParameters();
        return method.IsGenericMethodDefinition && parameters.Select(p => p.ParameterType).SequenceEqual(parameterTypes);
    }

    private class TestRestClient(HttpClient client) : Clc.Rest.RestClient(client)
    {
    }

    private sealed class RecordingFormatRestClient(HttpClient client) : TestRestClient(client)
    {
        public string LastContent { get; private set; }

        public override Task<T> FormatResponseAsync<T>(HttpResponseMessage response, string content, CancellationToken cancellationToken = default)
        {
            LastContent = content;
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

    private sealed class ThrowOnSecondReadContent(string body) : SingleReadTrackingContent(body)
    {
        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            if (ReadCount >= 1)
            {
                throw new InvalidOperationException("Content read more than once.");
            }

            return base.CreateContentReadStreamAsync();
        }
    }
}
