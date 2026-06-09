using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests.Models;

[TestClass]
public class RestRequestTests
{
    [TestMethod]
    public void DefaultConstructor_SetsExpectedDefaults()
    {
        var request = new RestRequest();

        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual(string.Empty, request.Path);
        Assert.IsNull(request.Body);
        Assert.IsNull(request.Content);
        Assert.IsNotNull(request.Headers);
        Assert.IsNotNull(request.QueryParameters);
        Assert.AreEqual(0, request.Headers.Count);
        Assert.AreEqual(0, request.QueryParameters.Count);
    }

    [TestMethod]
    public void Constructor_WithPathBodyAndQuery_SetsProperties()
    {
        var body = new { Id = 1 };
        var query = new Dictionary<string, object> { ["q"] = "test" };
        var request = new RestRequest("/test", body, query);

        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual("/test", request.Path);
        Assert.AreSame(body, request.Body);
        Assert.AreSame(query, request.QueryParameters);
    }

    [TestMethod]
    public void Constructor_WithMethodPathBodyAndQuery_SetsProperties()
    {
        var body = new { Id = 1 };
        var query = new Dictionary<string, object> { ["q"] = "test" };
        var request = new RestRequest(HttpMethod.Post, "/test", body, query);

        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("/test", request.Path);
        Assert.AreSame(body, request.Body);
        Assert.AreSame(query, request.QueryParameters);
    }

    [TestMethod]
    public void Get_Factory_SetsProperties()
    {
        var query = new Dictionary<string, object> { ["q"] = "test" };
        var request = RestRequest.Get("/test", query);

        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual("/test", request.Path);
        Assert.IsNull(request.Body);
        Assert.AreSame(query, request.QueryParameters);
    }

    [TestMethod]
    public void Delete_Factory_SetsProperties()
    {
        var query = new Dictionary<string, object> { ["q"] = "test" };
        var request = RestRequest.Delete("/test", query);

        Assert.AreEqual(HttpMethod.Delete, request.Method);
        Assert.AreEqual("/test", request.Path);
        Assert.IsNull(request.Body);
        Assert.AreSame(query, request.QueryParameters);
    }

    [TestMethod]
    public void Post_Factory_SetsProperties()
    {
        var body = new { Id = 1 };
        var query = new Dictionary<string, object> { ["q"] = "test" };
        var request = RestRequest.Post("/test", body, query);

        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("/test", request.Path);
        Assert.AreSame(body, request.Body);
        Assert.AreSame(query, request.QueryParameters);
    }

    [TestMethod]
    public void Put_Factory_SetsProperties()
    {
        var body = new { Id = 1 };
        var query = new Dictionary<string, object> { ["q"] = "test" };
        var request = RestRequest.Put("/test", body, query);

        Assert.AreEqual(HttpMethod.Put, request.Method);
        Assert.AreEqual("/test", request.Path);
        Assert.AreSame(body, request.Body);
        Assert.AreSame(query, request.QueryParameters);
    }

    [TestMethod]
    public void Patch_Factory_SetsProperties()
    {
        var body = new { Id = 1 };
        var query = new Dictionary<string, object> { ["q"] = "test" };
        var request = RestRequest.Patch("/test", body, query);

        Assert.AreEqual("PATCH", request.Method.Method);
        Assert.AreEqual("/test", request.Path);
        Assert.AreSame(body, request.Body);
        Assert.AreSame(query, request.QueryParameters);
    }

    [TestMethod]
    public void PostForm_Factory_SetsProperties()
    {
        var formValues = new Dictionary<string, string> { ["key"] = "value" };
        var query = new Dictionary<string, object> { ["q"] = "test" };
        var request = RestRequest.PostForm("/test", formValues, query);

        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("/test", request.Path);
        Assert.IsNull(request.Body);
        Assert.IsInstanceOfType(request.Content, typeof(FormUrlEncodedContent));
        Assert.AreSame(query, request.QueryParameters);
    }

    [TestMethod]
    public void WithContent_Factory_SetsProperties()
    {
        var content = new StringContent("test");
        var query = new Dictionary<string, object> { ["q"] = "test" };
        var request = RestRequest.WithContent(HttpMethod.Put, "/test", content, query);

        Assert.AreEqual(HttpMethod.Put, request.Method);
        Assert.AreEqual("/test", request.Path);
        Assert.IsNull(request.Body);
        Assert.AreSame(content, request.Content);
        Assert.AreSame(query, request.QueryParameters);
    }

    [TestMethod]
    public void Create_Factory_SetsProperties()
    {
        var body = new { Id = 1 };
        var query = new Dictionary<string, object> { ["q"] = "test" };
        var request = RestRequest.Create(HttpMethod.Options, "/test", body, query);

        Assert.AreEqual(HttpMethod.Options, request.Method);
        Assert.AreEqual("/test", request.Path);
        Assert.AreSame(body, request.Body);
        Assert.AreSame(query, request.QueryParameters);
    }
}