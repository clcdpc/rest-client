using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestRequestConstructorTests
{
    [TestMethod]
    public void RestRequest_DefaultConstructor_Has_Safe_Defaults()
    {
        var request = new RestRequest();

        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual(string.Empty, request.Path);
        Assert.IsNotNull(request.Headers);
        Assert.IsNotNull(request.QueryParameters);
        Assert.IsEmpty(request.Headers);
        Assert.IsEmpty(request.QueryParameters);
    }

    [TestMethod]
    public void RestRequest_Constructor_Normalizes_Null_Path_To_Empty_String()
    {
        var request = new RestRequest(HttpMethod.Get, null!);

        Assert.AreEqual(string.Empty, request.Path);
    }

    [TestMethod]
    public void RestRequest_Constructor_Normalizes_Null_Method_To_Get()
    {
        var request = new RestRequest(null!, "/items");

        Assert.AreEqual(HttpMethod.Get, request.Method);
    }

    [TestMethod]
    public void RestRequest_Headers_Setter_Normalizes_Null_To_Empty_Dictionary()
    {
        var request = new RestRequest
        {
            Headers = null!
        };

        Assert.IsNotNull(request.Headers);
        Assert.IsEmpty(request.Headers);
    }

    [TestMethod]
    public void RestRequest_QueryParameters_Setter_Normalizes_Null_To_Empty_Dictionary()
    {
        var request = new RestRequest
        {
            QueryParameters = null!
        };

        Assert.IsNotNull(request.QueryParameters);
        Assert.IsEmpty(request.QueryParameters);
    }
}
