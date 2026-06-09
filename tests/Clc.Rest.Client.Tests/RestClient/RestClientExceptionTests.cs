using System.Net;
using System.Net.Http;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientExceptionTests
{
    public required TestContext TestContext { get; set; }

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

        var response = await client.ExecuteAsync<string>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.IsInstanceOfType<HttpRequestException>(response.Exception);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_Deserialization_Fails_Captures_Exception()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{not-json"));
        var client = CreateClient(handler);

        var response = await client.ExecuteAsync<Payload>(RestRequest.Get("/data"), TestContext.CancellationToken);

        Assert.IsNotNull(response.Exception);
    }
}
