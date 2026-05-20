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
    public async Task Execute_Forwards_Body_And_Parameters_Correctly()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"ok\":true}"));
        var client = CreateClient(handler);
        var body = new { Name = "Alice" };
        var parameters = new Dictionary<string, string> { ["x"] = "1" };

        var response = await client.ExecuteAsync<Dictionary<string, object>>("/resource", HttpMethod.Post, parameters, body);

        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.AreEqual("{\"Name\":\"Alice\"}", await handler.LastRequest.Content!.ReadAsStringAsync(TestContext.CancellationToken));
        Assert.StartsWith("https://example.test/resource", handler.LastRequest.RequestUri!.AbsoluteUri);
        Assert.IsNotNull(response.Data);
    }

    [TestMethod]
    public async Task Get_Appends_Query_Parameters()
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
    public async Task Post_With_Json_Body_Does_Not_Lose_Body()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>("/post", HttpMethod.Post, new Dictionary<string, string> { ["a"] = "b" }, new { Id = 42 });

        var sentBody = await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.Contains("\"Id\":42", sentBody);
        Assert.AreEqual("application/json; charset=utf-8", handler.LastRequest.Content.Headers.ContentType!.ToString());
    }

    [TestMethod]
    public async Task Post_With_Form_Parameters_Uses_FormUrlEncoded_When_No_Body()
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
    [DataRow("q", "hello world", "q=hello%20world")]
    [DataRow("tag", "a&b=c", "tag=a%26b%3Dc")]
    public async Task Get_Encodes_Query_String_Values(string key, string value, string expectedFragment)
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>("/search", HttpMethod.Get, new Dictionary<string, string>
        {
            [key] = value
        });

        var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.Contains(expectedFragment, uri);
    }

    [TestMethod]
    public async Task Response_Content_Available_In_RestResponse_Response_Content()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"message\":\"ok\"}"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Dictionary<string, string>>("/data");

        Assert.AreEqual("{\"message\":\"ok\"}", response.Response.Content);
    }

    public static IEnumerable<object[]> FormatResponseCases()
    {
        yield return new object[] { "string", "hello" };
        yield return new object[] { "bool", "ignored" };
        yield return new object[] { "json", "{\"Name\":\"World\"}" };
    }

    [TestMethod]
    [DynamicData(nameof(FormatResponseCases))]
    public void FormatResponse_Returns_Expected_Output(string caseName, string payload)
    {
        var client = new TestRestClient(new HttpClient(new FakeHttpMessageHandler(_ => JsonResponse("{}"))));
        var response = JsonResponse(payload);

        if (caseName == "string")
            Assert.AreEqual("hello", client.FormatResponse<string>(response));
        else if (caseName == "bool")
            Assert.IsTrue(client.FormatResponse<bool>(response));
        else
            Assert.AreEqual("World", client.FormatResponse<Payload>(response).Name);
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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_callback(request));
        }
    }

    private sealed class TestRestClient(HttpClient client) : Clc.Rest.RestClient(client)
    {
    }

    private sealed class Payload
    {
        public string Name { get; set; } = string.Empty;
    }
}
