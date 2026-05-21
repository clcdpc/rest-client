using System.Net;
using System.Net.Http;
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

        await client.ExecuteAsync<string>("/post", HttpMethod.Post, body: new { Name = "Alice" });

        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.AreEqual("{\"Name\":\"Alice\"}", await handler.LastRequest.Content!.ReadAsStringAsync(TestContext.CancellationToken));
        Assert.AreEqual("application/json; charset=utf-8", handler.LastRequest.Content.Headers.ContentType!.ToString());
    }

    [TestMethod]
    public async Task Post_With_Parameters_And_No_Body_Uses_FormUrlEncodedContent()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>("/post", HttpMethod.Post, new Dictionary<string, string>
        {
            ["first"] = "one",
            ["second"] = "two"
        });

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

        await client.ExecuteAsync<string>("/post", HttpMethod.Post, new Dictionary<string, string> { ["a"] = "b" }, new { Id = 42 });

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

        await client.ExecuteAsync<string>("/search", HttpMethod.Get, new Dictionary<string, string>
        {
            ["q"] = "value",
            ["n"] = "10"
        });

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

        await client.ExecuteAsync<string>("/resource", new HttpMethod(method), new Dictionary<string, string>
        {
            ["x y"] = "a&b",
            ["p"] = "q"
        });

        var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.Contains("x%20y=a%26b", uri);
        Assert.Contains("p=q", uri);
    }

    [TestMethod]
    public async Task Query_String_Appending_Preserves_Existing_Query_And_Uses_Separators_Correctly()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>("/search?existing=1", HttpMethod.Get, new Dictionary<string, string>
        {
            ["new key"] = "new value"
        });

        var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.StartsWith("https://example.test/search?existing=1&", uri);
        Assert.Contains("new%20key=new%20value", uri);
    }


    [TestMethod]
    public async Task NonPost_With_Fragment_Appends_Query_Before_Fragment()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>("/resource#frag", HttpMethod.Put, new Dictionary<string, string>
        {
            ["x"] = "1"
        });

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

        await client.ExecuteAsync<string>("relative/path", HttpMethod.Delete, new Dictionary<string, string>
        {
            ["x"] = "1"
        });

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
    public async Task ExecuteAsync_Deserializes_Data_From_Same_Content_String()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"Name\":\"Shared\"}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Payload>("/data");

        Assert.AreEqual("Shared", response.Data.Name);
        Assert.AreEqual("{\"Name\":\"Shared\"}", response.Response.Content);
    }

    [TestMethod]
    public async Task ExecuteAsync_String_Response_Returns_Raw_Body()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("raw-body"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>("/data");

        Assert.AreEqual("raw-body", response.Data);
    }

    [TestMethod]
    public async Task ExecuteAsync_Bool_Response_Reflects_Success_Status()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<bool>("/data");

        Assert.IsTrue(response.Data);
    }

    [TestMethod]
    public async Task ExecuteAsync_Captures_Request_BodyString_When_Content_Exists()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<string>("/data", HttpMethod.Post, body: new { Name = "Body" });

        Assert.AreEqual("{\"Name\":\"Body\"}", response.BodyString);
    }

    [TestMethod]
    public async Task ExecuteAsync_Reads_Response_Content_Only_Once()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new CountingContent("{\"Name\":\"Once\"}") });
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Payload>("/data");
        var countingContent = (CountingContent)handler.LastResponse!.Content!;

        Assert.AreEqual(1, countingContent.ReadCount);
        Assert.AreEqual("Once", response.Data.Name);
        Assert.AreEqual("{\"Name\":\"Once\"}", response.Response.Content);
    }

    public enum FormatResponseCase
    {
        String,
        Bool,
        Json
    }

    [TestMethod]
    [DataRow(FormatResponseCase.String, "hello", "hello")]
    [DataRow(FormatResponseCase.Bool, "ignored", "true")]
    [DataRow(FormatResponseCase.Json, "{\"Name\":\"World\"}", "World")]
    public void FormatResponse_Returns_Expected_Output(FormatResponseCase caseName, string payload, string expectedValue)
    {
        var client = new TestRestClient(new HttpClient(new FakeHttpMessageHandler(_ => JsonResponse("{}"))));
        var response = JsonResponse(payload);

        if (caseName == FormatResponseCase.String)
        {
            Assert.AreEqual(expectedValue, client.FormatResponse<string>(response));
        }
        else if (caseName == FormatResponseCase.Bool)
        {
            Assert.AreEqual(bool.Parse(expectedValue), client.FormatResponse<bool>(response));
        }
        else
        {
            Assert.AreEqual(expectedValue, client.FormatResponse<Payload>(response).Name);
        }
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
        public HttpResponseMessage? LastResponse { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastResponse = _callback(request);
            return Task.FromResult(LastResponse);
        }
    }

    private sealed class TestRestClient(HttpClient client) : Clc.Rest.RestClient(client)
    {
    }

    private sealed class Payload
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class CountingContent(string value) : HttpContent
    {
        private readonly string _value = value;
        public int ReadCount { get; private set; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            throw new NotSupportedException();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _value.Length;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            ReadCount++;
            var bytes = Encoding.UTF8.GetBytes(_value);
            return Task.FromResult<Stream>(new MemoryStream(bytes));
        }
    }
}
