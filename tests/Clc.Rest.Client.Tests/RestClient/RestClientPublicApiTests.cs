using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientPublicApiTests
{
    [TestMethod]
    public void Public_Async_Api_Shape_Is_Simplified()
    {
        var methods = typeof(Clc.Rest.RestClient).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var executeAsync = methods.Where(m => m.Name == "ExecuteAsync").ToList();
        var buildRequestUri = methods.Where(m => m.Name == "BuildRequestUri").ToList();

        Assert.HasCount(1, executeAsync);
        Assert.IsNotNull(buildRequestUri.SingleOrDefault(m =>
            m.ReturnType == typeof(Uri)
            && m.GetParameters().Length == 1
            && m.GetParameters()[0].ParameterType == typeof(RestRequest)));
        Assert.IsNotNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(RestRequest)
            && m.GetParameters()[1].ParameterType == typeof(CancellationToken)));
        Assert.IsNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(string)
            && m.GetParameters()[1].ParameterType == typeof(CancellationToken)));
        Assert.IsNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 3
            && m.GetParameters()[0].ParameterType == typeof(HttpMethod)
            && m.GetParameters()[1].ParameterType == typeof(string)
            && m.GetParameters()[2].ParameterType == typeof(CancellationToken)));

        var names = methods.Select(m => m.Name).ToList();
        Assert.AreEqual(typeof(Dictionary<string, object>), typeof(RestRequest).GetProperty("QueryParameters")!.PropertyType);
        Assert.IsNull(typeof(RestRequest).GetProperty("Parameters"));
        Assert.IsNull(typeof(RestRequest).GetProperty("FormParameters"));

        var postForm = typeof(RestRequest).GetMethod("PostForm", [typeof(string), typeof(Dictionary<string, string>), typeof(Dictionary<string, object>)]);
        Assert.IsNotNull(postForm);

        foreach (var methodName in new[] { "Get", "Post", "Put", "Patch", "Delete", "Create", "WithContent" })
        {
            var overloads = typeof(RestRequest).GetMethods().Where(m => m.Name == methodName).ToList();
            Assert.IsNotNull(overloads.SingleOrDefault(m => m.GetParameters().Any(p => p.Name == "queryParameters" && p.ParameterType == typeof(Dictionary<string, object>))));
        }

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

        Assert.HasCount(1, executeAsync);
        Assert.IsNotNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(RestRequest)
            && m.GetParameters()[1].ParameterType == typeof(CancellationToken)));
        Assert.IsNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(string)
            && m.GetParameters()[1].ParameterType == typeof(CancellationToken)));
        Assert.IsNull(executeAsync.SingleOrDefault(m =>
            m.IsGenericMethodDefinition
            && m.GetParameters().Length == 3
            && m.GetParameters()[0].ParameterType == typeof(HttpMethod)
            && m.GetParameters()[1].ParameterType == typeof(string)
            && m.GetParameters()[2].ParameterType == typeof(CancellationToken)));

        var names = methods.Select(m => m.Name).ToList();
        Assert.AreEqual(typeof(Dictionary<string, object>), typeof(RestRequest).GetProperty("QueryParameters")!.PropertyType);
        Assert.IsNull(typeof(RestRequest).GetProperty("Parameters"));
        Assert.IsNull(typeof(RestRequest).GetProperty("FormParameters"));

        var postForm = typeof(RestRequest).GetMethod("PostForm", [typeof(string), typeof(Dictionary<string, string>), typeof(Dictionary<string, object>)]);
        Assert.IsNotNull(postForm);

        foreach (var methodName in new[] { "Get", "Post", "Put", "Patch", "Delete", "Create", "WithContent" })
        {
            var overloads = typeof(RestRequest).GetMethods().Where(m => m.Name == methodName).ToList();
            Assert.IsNotNull(overloads.SingleOrDefault(m => m.GetParameters().Any(p => p.Name == "queryParameters" && p.ParameterType == typeof(Dictionary<string, object>))));
        }

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
    public void HttpResponse_Does_Not_Expose_Sync_Content_Read_Constructor()
    {
        var constructors = typeof(HttpResponse).GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        Assert.IsNull(constructors.SingleOrDefault(c =>
            c.GetParameters().Length == 1
            && c.GetParameters()[0].ParameterType == typeof(HttpResponseMessage)));
        Assert.IsNotNull(constructors.SingleOrDefault(c =>
            c.GetParameters().Length == 2
            && c.GetParameters()[0].ParameterType == typeof(HttpResponseMessage)
            && c.GetParameters()[1].ParameterType == typeof(string)));
        Assert.IsNotNull(constructors.SingleOrDefault(c => c.GetParameters().Length == 0));
    }

    [TestMethod]
    public void Public_Api_Does_Not_Expose_Sync_Execution_Or_Sync_Content_Reads()
    {
        var restClientMethods = typeof(Clc.Rest.RestClient).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var restClientMethodNames = restClientMethods.Select(m => m.Name).ToList();
        Assert.DoesNotContain("Execute", restClientMethodNames);

        var iRestClientMethods = typeof(IRestClient).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var iRestClientMethodNames = iRestClientMethods.Select(m => m.Name).ToList();
        Assert.DoesNotContain("Execute", iRestClientMethodNames);

        var httpResponseConstructors = typeof(HttpResponse).GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.IsNull(httpResponseConstructors.SingleOrDefault(c =>
            c.GetParameters().Length == 1
            && c.GetParameters()[0].ParameterType == typeof(HttpResponseMessage)));

        var publicHttpResponseMethods = typeof(HttpResponse).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
        Assert.IsNull(publicHttpResponseMethods.SingleOrDefault(m => m.Name == "ReadContentSynchronously"));
    }
}
