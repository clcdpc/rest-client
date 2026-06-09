using System.Globalization;
using System.Net.Http;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientRequestUriTests
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task ExecuteAsync_Uses_Get_Factory_Request_With_QueryParameters()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Get, "/search", body: null, queryParameters: new Dictionary<string, object>
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
    public async Task NonPost_Methods_With_QueryParameters_Append_Query_String(string method)
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(new RestRequest(new HttpMethod(method), "/resource", body: null, queryParameters: new Dictionary<string, object>
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

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Get, "/search?existing=1", body: null, queryParameters: new Dictionary<string, object>
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

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Put, "/resource#frag", body: null, queryParameters: new Dictionary<string, object>
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

        await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Delete, "relative/path", body: null, queryParameters: new Dictionary<string, object>
        {
            ["x"] = "1"
        }), TestContext.CancellationToken);

        Assert.AreEqual("https://example.test/relative/path?x=1", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public void BuildUrl_Returns_Absolute_Path_Unchanged_When_BaseUrl_Is_Set()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        client.BaseUrl = "https://api.example.com";

        var result = client.BuildUrl(new RestRequest(HttpMethod.Get, "https://other.example.com/items"));

        Assert.AreEqual("https://other.example.com/items", result);
    }

    [TestMethod]
    public void BuildUrl_Returns_Absolute_Path_Unchanged_When_PathPrefix_Is_Set()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        client.BaseUrl = "https://api.example.com";
        client.PathPrefix = "v1";

        var result = client.BuildUrl(new RestRequest(HttpMethod.Get, "https://other.example.com/items?existing=true"));

        Assert.AreEqual("https://other.example.com/items?existing=true", result);
    }

    [TestMethod]
    public void BuildRequestUri_When_Request_Is_Null_Throws_ArgumentNullException()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        Assert.ThrowsExactly<ArgumentNullException>(() => client.BuildRequestUri(null!));
    }

    [TestMethod]
    public async Task BuildRequestUri_With_No_QueryParameters_Matches_ExecuteAsync_Sent_Uri()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = RestRequest.Get("/items");

        var builtUri = client.BuildRequestUri(request);
        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual(builtUri, handler.LastRequest!.RequestUri);
        Assert.AreEqual("https://example.test/items", builtUri.AbsoluteUri);
    }

    [TestMethod]
    public void BuildRequestUri_Appends_And_Escapes_QueryParameters()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = RestRequest.Get("/search", new Dictionary<string, object>
        {
            ["query value"] = "spaces & symbols",
            ["page"] = 2
        });

        var uri = client.BuildRequestUri(request).AbsoluteUri;

        Assert.StartsWith("https://example.test/search?", uri);
        Assert.Contains("query%20value=spaces%20%26%20symbols", uri);
        Assert.Contains("page=2", uri);
    }

    [TestMethod]
    public void BuildRequestUri_Omits_Null_Empty_Blank_Values_And_Blank_Keys()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = RestRequest.Get("/items", new Dictionary<string, object>
        {
            ["keep"] = "value",
            ["nullValue"] = null!,
            ["empty"] = string.Empty,
            ["blank"] = "   ",
            [""] = "empty-key",
            ["   "] = "blank-key"
        });

        var uri = client.BuildRequestUri(request).AbsoluteUri;

        Assert.AreEqual("https://example.test/items?keep=value", uri);
    }

    [TestMethod]
    public void BuildRequestUri_Converts_QueryParameter_Values_Using_Invariant_Culture()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var request = RestRequest.Get("/items", new Dictionary<string, object>
            {
                ["price"] = 12.34m,
                ["includeDeleted"] = false
            });

            var uri = client.BuildRequestUri(request).AbsoluteUri;

            Assert.Contains("price=12.34", uri);
            Assert.Contains("includeDeleted=False", uri);
            Assert.DoesNotContain("price=12%2C34", uri);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [TestMethod]
    public void BuildRequestUri_Preserves_Existing_Query_And_Appends_With_Ampersand()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = RestRequest.Get("/search?existing=1", new Dictionary<string, object>
        {
            ["new key"] = "new value"
        });

        var uri = client.BuildRequestUri(request).AbsoluteUri;

        Assert.AreEqual("https://example.test/search?existing=1&new%20key=new%20value", uri);
    }

    [TestMethod]
    public void BuildRequestUri_Inserts_QueryParameters_Before_Fragment()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = RestRequest.Get("/resource#frag", new Dictionary<string, object>
        {
            ["x"] = "1"
        });

        var uri = client.BuildRequestUri(request).AbsoluteUri;

        Assert.AreEqual("https://example.test/resource?x=1#frag", uri);
    }

    [TestMethod]
    public void BuildRequestUri_Preserves_Absolute_Url_Path_When_BaseUrl_And_PathPrefix_Are_Set()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        client.BaseUrl = "https://api.example.com";
        client.PathPrefix = "v1";
        var request = RestRequest.Get("https://other.example.com/items/42?existing=true", new Dictionary<string, object>
        {
            ["q"] = "hello world"
        });

        var uri = client.BuildRequestUri(request).AbsoluteUri;

        Assert.AreEqual("https://other.example.com/items/42?existing=true&q=hello%20world", uri);
    }

    [TestMethod]
    public async Task BuildRequestUri_Relative_Url_Works_With_HttpClient_BaseAddress()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://base-address.example/")
        };
        var client = new TestRestClient(httpClient) { BaseUrl = string.Empty };
        var request = RestRequest.Get("relative/path", new Dictionary<string, object>
        {
            ["x"] = "1"
        });

        var builtUri = client.BuildRequestUri(request);
        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.IsFalse(builtUri.IsAbsoluteUri);
        Assert.AreEqual("relative/path?x=1", builtUri.OriginalString);
        Assert.AreEqual("https://base-address.example/relative/path?x=1", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task ExecuteAsync_Sends_Exact_Uri_Returned_By_BuildRequestUri()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var request = RestRequest.Get("/items?existing=1#frag", new Dictionary<string, object>
        {
            ["new"] = "value"
        });

        var builtUri = client.BuildRequestUri(request);
        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual(builtUri, handler.LastRequest!.RequestUri);
        Assert.AreEqual("https://example.test/items?existing=1&new=value#frag", builtUri.AbsoluteUri);
    }

    [TestMethod]
    public async Task ExecuteAsync_Uses_Get_Factory_Request_With_Absolute_Url_When_BaseUrl_Is_Set()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        client.BaseUrl = "https://api.example.com";

        var response = await client.ExecuteAsync<string>(RestRequest.Get("https://other.example.com/items"), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("https://other.example.com/items", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task ExecuteAsync_Uses_Create_Factory_Request_With_Absolute_Url_When_BaseUrl_And_PathPrefix_Are_Set()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        client.BaseUrl = "https://api.example.com";
        client.PathPrefix = "v1";

        var response = await client.ExecuteAsync<string>(RestRequest.Create(HttpMethod.Post, "https://other.example.com/items"), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("https://other.example.com/items", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task ExecuteAsync_QueryParameters_Append_To_Absolute_Url()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        client.BaseUrl = "https://api.example.com";
        client.PathPrefix = "v1";

        var response = await client.ExecuteAsync<string>(new RestRequest(HttpMethod.Get, "https://other.example.com/items?existing=true", body: null, queryParameters: new Dictionary<string, object>
        {
            ["q"] = "hello world"
        }), TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        Assert.AreEqual("https://other.example.com/items?existing=true&q=hello%20world", handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task ExecuteAsync_QueryParameters_Convert_Object_Values_Using_Invariant_Culture()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            var request = RestRequest.Get("/items", new Dictionary<string, object>
            {
                ["page"] = 2,
                ["includeDeleted"] = false,
                ["price"] = 12.34m
            });

            var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

            Assert.IsNull(response.Exception);
            var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
            Assert.Contains("page=2", uri);
            Assert.Contains("includeDeleted=False", uri);
            Assert.Contains("price=12.34", uri);
            Assert.DoesNotContain("price=12%2C34", uri);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_QueryParameters_Skip_Null_And_Empty_Object_Values()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        var request = RestRequest.Get("/items", new Dictionary<string, object>
        {
            ["keep"] = "value",
            ["nullValue"] = null!,
            ["empty"] = string.Empty,
            ["whitespace"] = "   ",
            ["sp ace"] = "a&b"
        });

        var response = await client.ExecuteAsync<string>(request, TestContext.CancellationToken);

        Assert.IsNull(response.Exception);
        var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.Contains("keep=value", uri);
        Assert.Contains("sp%20ace=a%26b", uri);
        Assert.DoesNotContain("nullValue=", uri);
        Assert.DoesNotContain("empty=", uri);
        Assert.DoesNotContain("whitespace=", uri);
    }
}
