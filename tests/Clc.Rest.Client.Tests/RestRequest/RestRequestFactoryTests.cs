using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestRequestFactoryTests
{
    [TestMethod]
    public void RestRequest_PostForm_Creates_Post_Request_With_FormUrlEncodedContent()
    {
        var request = RestRequest.PostForm("/token", new Dictionary<string, string> { ["grant_type"] = "client_credentials" });

        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.IsNull(request.Body);
        Assert.IsNotNull(request.Content);
    }

    [TestMethod]
    public void RestRequest_WithContent_Creates_Request_With_Explicit_Content()
    {
        var content = new StringContent("abc", Encoding.UTF8, "text/plain");
        var request = RestRequest.WithContent(HttpMethod.Put, "/items", content);

        Assert.AreEqual(HttpMethod.Put, request.Method);
        Assert.AreSame(content, request.Content);
        Assert.IsNull(request.Body);
    }

    [TestMethod]
    public void RestRequest_Factories_Accept_Object_QueryParameter_Values()
    {
        var queryParameters = new Dictionary<string, object> { ["page"] = 2, ["includeDeleted"] = false };

        RestRequest[] requests =
        [
            RestRequest.Get("/items", queryParameters),
            RestRequest.Delete("/items", queryParameters),
            RestRequest.Post("/items", new { Name = "x" }, queryParameters),
            RestRequest.Put("/items", new { Name = "x" }, queryParameters),
            RestRequest.Patch("/items", new { Name = "x" }, queryParameters),
            RestRequest.Create(HttpMethod.Trace, "/items", null, queryParameters),
            RestRequest.WithContent(HttpMethod.Post, "/items", new StringContent("x"), queryParameters)
        ];

        foreach (var request in requests)
        {
            Assert.AreSame(queryParameters, request.QueryParameters);
        }
    }
}
