using System.Net;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Linq;
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
        }), TestContext.CancellationToken);

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

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/post", new { Id = 42 }, new Dictionary<string, string> { ["a"] = "b" }), TestContext.CancellationToken);

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
        }), TestContext.CancellationToken);

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
        }), TestContext.CancellationToken);

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
        }), TestContext.CancellationToken);

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
        }), TestContext.CancellationToken);

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
        }), TestContext.CancellationToken);

        Assert.AreEqual("https://example.test/relative/path?x=1", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task Response_Content_Available_In_RestResponse_Response_Content()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"message\":\"ok\"}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Dictionary<string, string>>("/data", TestContext.CancellationToken);

        Assert.AreEqual("{\"message\":\"ok\"}", response.Response.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_String_Response_Uses_Raw_Body()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("plain-text"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>("/data", TestContext.CancellationToken);

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

        var response = await client.ExecuteAsync<bool>("/data", TestContext.CancellationToken);

        Assert.IsTrue(response.Data);
    }

    [TestMethod]
    public async Task ExecuteAsync_Uses_Same_Content_For_Metadata_And_Deserialization()
    {
        var payload = "{\"Name\":\"FromBody\"}";
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(payload));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Payload>("/data", TestContext.CancellationToken);

        Assert.AreEqual(payload, response.Response.Content);
        Assert.AreEqual("FromBody", response.Data.Name);
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
    public async Task ExecuteAsync_Sends_Request_Returned_By_Authenticator()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var replacementRequest = new HttpRequestMessage(HttpMethod.Get, "https://example.test/replaced");
        replacementRequest.Headers.Add("X-Replaced", "true");
        var request = new RestRequest(HttpMethod.Get, "/original")
        {
            Authenticator = new ReplacingAuthenticator(replacementRequest)
        };

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreSame(replacementRequest, handler.LastRequest);
        Assert.AreSame(replacementRequest, response.Request);
        Assert.AreEqual("https://example.test/replaced", handler.LastRequest!.RequestUri!.AbsoluteUri);
        Assert.IsTrue(handler.LastRequest.Headers.Contains("X-Replaced"));
    }

    [TestMethod]
    public async Task ExecuteAsync_Uses_Final_Request_Returned_By_Request_Building_Hooks()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var replacementRequest = new HttpRequestMessage(HttpMethod.Get, "https://example.test/hook-replaced");
        replacementRequest.Headers.Add("X-Hook-Replaced", "yes");
        var client = new HookReplacingRestClient(new HttpClient(handler), replacementRequest) { BaseUrl = "https://example.test" };

        var response = await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Get, "/original"), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreSame(replacementRequest, handler.LastRequest);
        Assert.AreSame(replacementRequest, response.Request);
        Assert.IsTrue(handler.LastRequest!.Headers.Contains("X-Hook-Replaced"));
    }

    [TestMethod]
    public async Task ExecuteAsync_BodyString_Comes_From_Final_Request()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var replacementRequest = new HttpRequestMessage(HttpMethod.Post, "https://example.test/replaced-body")
        {
            Content = new StringContent("replacement-body", Encoding.UTF8, "text/plain")
        };
        var request = new RestRequest(HttpMethod.Post, "/original", body: new { Name = "Original" });
        var client = new ParameterHookReplacingRestClient(new HttpClient(handler), replacementRequest) { BaseUrl = "https://example.test" };

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("replacement-body", response.BodyString);
        Assert.AreSame(replacementRequest, handler.LastRequest);
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
        Assert.AreEqual("{\"Name\":\"Once\"}", response.Response.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_Passes_CancellationToken_To_HttpMessageHandler()
    {
        var tokenSource = new CancellationTokenSource();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>("/data", cancellationToken: tokenSource.Token);

        Assert.IsTrue(handler.LastCancellationToken.CanBeCanceled);
        Assert.AreNotEqual(CancellationToken.None, handler.LastCancellationToken);
    }

    [TestMethod]
    public async Task ExecuteAsync_FormatOutputAsync_Receives_Already_Read_Content()
    {
        var content = new ThrowOnSecondReadContent("from formatter");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var client = CreateClient(handler);
        var request = new RestRequest(HttpMethod.Get, "/data");
        string capturedContent = string.Empty;
        CancellationToken capturedToken = default;

        request.FormatOutputAsync = (_, formatterContent, cancellationToken) =>
        {
            capturedContent = formatterContent;
            capturedToken = cancellationToken;
            return Task.FromResult<object>($"formatted:{formatterContent}");
        };

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("formatted:from formatter", response.Data);
        Assert.AreEqual("from formatter", capturedContent);
        Assert.AreEqual(TestContext.CancellationToken, capturedToken);
        Assert.AreEqual(1, content.ReadCount);
        Assert.AreEqual("from formatter", response.Response.Content);
    }


    [TestMethod]
    public async Task ExecuteAsync_When_Cancelled_Before_Send_Does_Not_Run_Authenticator_Or_Serializer()
    {
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var authenticator = new TrackingAuthenticator();
        var serializer = new TrackingSerializer();
        var client = CreateClient(handler);
        var request = new RestRequest(HttpMethod.Post, "/data", body: new { Name = "Body" })
        {
            Authenticator = authenticator,
            Serializer = serializer
        };

        var response = await client.ExecuteAsync<string>(request, tokenSource.Token);

        Assert.IsInstanceOfType<OperationCanceledException>(response.Exception);
        Assert.IsFalse(authenticator.WasCalled);
        Assert.IsFalse(serializer.WasCalled);
        Assert.IsNull(handler.LastRequest);
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

        var response = await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Post, "/data", body: new { Name = "Body" }), tokenSource.Token);

        Assert.IsInstanceOfType<OperationCanceledException>(response.Exception);
        Assert.IsNull(handler.LastRequest);
    }


    [TestMethod]
    public async Task ExecuteAsync_When_Request_Is_Null_Captures_ArgumentNullException()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>((RestRequest)null!, TestContext.CancellationToken);

        Assert.IsInstanceOfType<ArgumentNullException>(response.Exception);
        Assert.IsNull(handler.LastRequest);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_Serializer_Throws_During_AddBody_Captures_Exception()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = new RestRequest(HttpMethod.Post, "/data", body: new { Name = "Alice" })
        {
            Serializer = new ThrowingSerializer(new InvalidOperationException("serialize fail"))
        };

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsInstanceOfType<InvalidOperationException>(response.Exception);
        Assert.IsNull(handler.LastRequest);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_Authenticator_Throws_During_AddAuthenticator_Captures_Exception()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = new RestRequest(HttpMethod.Get, "/data")
        {
            Authenticator = new ThrowingAuthenticator(new InvalidOperationException("auth fail"))
        };

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsInstanceOfType<InvalidOperationException>(response.Exception);
        Assert.IsNull(handler.LastRequest);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_SendAsync_Throws_HttpRequestException_Captures_Exception()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("network"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>("/data", TestContext.CancellationToken);

        Assert.IsInstanceOfType<HttpRequestException>(response.Exception);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_Deserialization_Fails_Captures_Exception()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{not-json"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Payload>("/data", TestContext.CancellationToken);

        Assert.IsNotNull(response.Exception);
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

        var response = await client.ExecuteAsync<Payload>("/data", TestContext.CancellationToken);

        Assert.IsTrue(responseMessage.IsDisposed);
        Assert.IsTrue(content.IsDisposed);
        Assert.AreEqual("Disposed", response.Data.Name);
        Assert.AreEqual("{\"Name\":\"Disposed\"}", response.Response.Content);
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

        var response = await client.ExecuteAsync<string>("/data", TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.IsTrue(responseMessage.IsDisposed);
        Assert.AreEqual("plain-text", response.Response.Content);
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

    [TestMethod]
    public void Public_Async_Api_Shape_Is_Simplified()
    {
        var methods = typeof(Clc.Rest.RestClient).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var executeAsync = methods.Where(m => m.Name == "ExecuteAsync").ToList();

        Assert.HasCount(3, executeAsync);
        Assert.IsNotNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(RestRequest)
            && m.GetParameters()[1].ParameterType == typeof(CancellationToken)));
        Assert.IsNotNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(string)
            && m.GetParameters()[1].ParameterType == typeof(CancellationToken)));
        Assert.IsNotNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 3
            && m.GetParameters()[0].ParameterType == typeof(HttpMethod)
            && m.GetParameters()[1].ParameterType == typeof(string)
            && m.GetParameters()[2].ParameterType == typeof(CancellationToken)));

        var names = methods.Select(m => m.Name).ToList();
        Assert.DoesNotContain("GetAsync", names);
        Assert.DoesNotContain("PostAsync", names);
        Assert.DoesNotContain("PutAsync", names);
        Assert.DoesNotContain("PatchAsync", names);
        Assert.DoesNotContain("DeleteAsync", names);
        Assert.DoesNotContain("FormatResponse", names);
        Assert.DoesNotContain("IsFormatResponseOverridden", names);
        Assert.DoesNotContain("CreateCompatibilityResponse", names);
        Assert.DoesNotContain("Execute", names);
    }

    [TestMethod]
    public void IRestClient_Public_Async_Api_Shape_Is_Simplified()
    {
        var methods = typeof(IRestClient).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var executeAsync = methods.Where(m => m.Name == "ExecuteAsync").ToList();

        Assert.HasCount(3, executeAsync);
        Assert.IsNotNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(RestRequest)
            && m.GetParameters()[1].ParameterType == typeof(CancellationToken)));
        Assert.IsNotNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(string)
            && m.GetParameters()[1].ParameterType == typeof(CancellationToken)));
        Assert.IsNotNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 3
            && m.GetParameters()[0].ParameterType == typeof(HttpMethod)
            && m.GetParameters()[1].ParameterType == typeof(string)
            && m.GetParameters()[2].ParameterType == typeof(CancellationToken)));

        var names = methods.Select(m => m.Name).ToList();
        Assert.DoesNotContain("GetAsync", names);
        Assert.DoesNotContain("PostAsync", names);
        Assert.DoesNotContain("PutAsync", names);
        Assert.DoesNotContain("PatchAsync", names);
        Assert.DoesNotContain("DeleteAsync", names);
        Assert.DoesNotContain("Execute", names);
        Assert.DoesNotContain("FormatResponse", names);
        Assert.DoesNotContain("IsFormatResponseOverridden", names);
        Assert.DoesNotContain("CreateCompatibilityResponse", names);
    }

    [TestMethod]
    public async Task IRestClient_ExecuteAsync_Can_Call_Concrete_RestClient()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("expected-value", Encoding.UTF8, "text/plain")
        });
        IRestClient client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>("/data", TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("expected-value", response.Data);
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


    private sealed class TrackingAuthenticator : Clc.Rest.Auth.IAuthenticator
    {
        public bool WasCalled { get; private set; }

        public HttpRequestMessage Authenticate(HttpClient client, HttpRequestMessage request)
        {
            WasCalled = true;
            return request;
        }
    }

    private sealed class ThrowingAuthenticator(Exception exception) : Clc.Rest.Auth.IAuthenticator
    {
        private readonly Exception _exception = exception;

        public HttpRequestMessage Authenticate(HttpClient client, HttpRequestMessage request) => throw _exception;
    }

    private sealed class ReplacingAuthenticator(HttpRequestMessage replacementRequest) : Clc.Rest.Auth.IAuthenticator
    {
        private readonly HttpRequestMessage _replacementRequest = replacementRequest;
        public HttpRequestMessage Authenticate(HttpClient client, HttpRequestMessage request) => _replacementRequest;
    }

    private sealed class HookReplacingRestClient(HttpClient client, HttpRequestMessage replacementRequest) : Clc.Rest.RestClient(client)
    {
        private readonly HttpRequestMessage _replacementRequest = replacementRequest;

        protected override HttpRequestMessage AddHeaders(RestRequest request, HttpRequestMessage httpRequest) => _replacementRequest;
    }

    private sealed class ParameterHookReplacingRestClient(HttpClient client, HttpRequestMessage replacementRequest) : Clc.Rest.RestClient(client)
    {
        private readonly HttpRequestMessage _replacementRequest = replacementRequest;

        protected override HttpRequestMessage AddParameters(RestRequest request, HttpRequestMessage httpRequest) => _replacementRequest;
    }

    private sealed class TrackingSerializer : Clc.Rest.ISerializer
    {
        public bool WasCalled { get; private set; }
        public string MediaType => "application/json";

        public string Serialize(object body, bool ignoreNullValues = true)
        {
            WasCalled = true;
            return "{}";
        }
    }

    private sealed class ThrowingSerializer(Exception exception) : Clc.Rest.ISerializer
    {
        private readonly Exception _exception = exception;
        public string MediaType => "application/json";

        public string Serialize(object body, bool ignoreNullValues = true) => throw _exception;
    }

    private sealed class Payload
    {
        public string Name { get; set; } = string.Empty;
    }


    private sealed class DisposableTrackingHttpResponseMessage(HttpStatusCode statusCode) : HttpResponseMessage(statusCode)
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class DisposableTrackingContent : StringContent
    {
        public DisposableTrackingContent(string content) : base(content, Encoding.UTF8, "application/json")
        {
        }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class ThrowOnSecondReadContent : HttpContent
    {
        private readonly byte[] _payloadBytes;

        public ThrowOnSecondReadContent(string body)
        {
            _payloadBytes = Encoding.UTF8.GetBytes(body);
            Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
            {
                CharSet = Encoding.UTF8.WebName
            };
        }

        public int ReadCount { get; private set; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            ReadCount++;
            if (ReadCount > 1)
            {
                throw new InvalidOperationException("Content stream was read more than once.");
            }

            return stream.WriteAsync(_payloadBytes, 0, _payloadBytes.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _payloadBytes.Length;
            return true;
        }
    }
}
