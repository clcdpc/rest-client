using Clc.Rest;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientExecuteTests
{
    [DataTestMethod]
    [DataRow("/default-method", DisplayName = "Defaults to GET when method omitted")]
    [DataRow("/default-method-2", DisplayName = "Defaults to GET for another url")]
    public void Execute_WithStringUrlOverload_UsesGetAsDefaultMethod(string url)
    {
        var handler = CreateHandler();
        var client = new TestRestClient(new HttpClient(handler));

        _ = client.Execute<bool>(url);

        Assert.IsNotNull(handler.LastRequest);
        Assert.AreEqual(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.AreEqual(url, handler.LastRequest.RequestUri!.PathAndQuery);
    }

    [DataTestMethod]
    [DataRow("/endpoint", "q", "a value", "example", true, "/endpoint?q=a%20value", DisplayName = "Preserves body and query")]
    [DataRow("/body-only", "", "", "body-only", true, "/body-only", DisplayName = "Preserves body without query")]
    [DataRow("/params-only", "search", "space value", "", false, "/params-only?search=space%20value", DisplayName = "Preserves query without body")]
    [DataRow("/none", "", "", "", false, "/none", DisplayName = "Handles no body and no query")]
    public async Task Execute_WithStringUrlOverload_PreservesBodyAndQueryAcrossCombinations(
        string url,
        string queryKey,
        string queryValue,
        string bodyName,
        bool expectBody,
        string expectedPathAndQuery)
    {
        var handler = CreateHandler();
        var client = new TestRestClient(new HttpClient(handler));

        var parameters = string.IsNullOrWhiteSpace(queryKey)
            ? null
            : new Dictionary<string, string> { [queryKey] = queryValue };

        object body = string.IsNullOrWhiteSpace(bodyName)
            ? null
            : new { Name = bodyName };

        _ = client.Execute<bool>(url, HttpMethod.Get, parameters, body);

        Assert.IsNotNull(handler.LastRequest);
        Assert.AreEqual(expectedPathAndQuery, handler.LastRequest!.RequestUri!.PathAndQuery);

        if (expectBody)
        {
            Assert.IsNotNull(handler.LastRequest.Content);
            var content = await handler.LastRequest.Content!.ReadAsStringAsync();
            StringAssert.Contains(content, $"\"Name\":\"{bodyName}\"");
        }
        else
        {
            Assert.IsNull(handler.LastRequest.Content);
        }
    }

    [DataTestMethod]
    [DataRow("/captured-post", "k", "v", "captured", "POST", DisplayName = "Forwards body + params for POST")]
    [DataRow("/captured-get", "q", "x", "captured-get", "GET", DisplayName = "Forwards body + params for GET")]
    public void Execute_WithStringUrlOverload_ForwardsBodyAndParametersIntoRestRequest(
        string url,
        string queryKey,
        string queryValue,
        string bodyName,
        string method)
    {
        var handler = CreateHandler();
        var client = new CapturingRestClient(new HttpClient(handler));
        var parameters = new Dictionary<string, string>
        {
            [queryKey] = queryValue
        };
        var body = new { Name = bodyName };

        _ = client.Execute<bool>(url, new HttpMethod(method), parameters, body);

        Assert.IsNotNull(client.CapturedRequest);
        Assert.AreSame(body, client.CapturedRequest!.Body);
        Assert.AreSame(parameters, client.CapturedRequest.Parameters);
        Assert.AreEqual(method, client.CapturedRequest.Method.Method);
        Assert.AreEqual(url, client.CapturedRequest.Path);
    }

    private static StubHttpMessageHandler CreateHandler() => new(_ =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("true")
        });

    private sealed class TestRestClient : RestClient
    {
        public TestRestClient(HttpClient client) : base(client)
        {
        }
    }

    private sealed class CapturingRestClient : RestClient
    {
        public CapturingRestClient(HttpClient client) : base(client)
        {
        }

        public RestRequest? CapturedRequest { get; private set; }

        public override RestRequest PreformatRestRequest(RestRequest request)
        {
            CapturedRequest = request;
            return base.PreformatRestRequest(request);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responseFactory(request));
        }
    }
}
