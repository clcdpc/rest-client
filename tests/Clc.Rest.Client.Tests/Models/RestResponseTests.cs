using System;
using System.Net.Http;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests.Models;

[TestClass]
public class RestResponseTests
{
    [TestMethod]
    public void RestResponse_DefaultConstructor_CreatesEmptyResponse()
    {
        var response = new RestResponse<string>();

        Assert.IsNull(response.Data);
        Assert.IsNull(response.Exception);
        Assert.IsNull(response.BodyString);
        Assert.AreEqual(0, response.ResponseTime);
        Assert.IsNotNull(response.Response);
        Assert.IsNotNull(response.Request);
    }

    [TestMethod]
    public void RestResponse_DataConstructor_SetsData()
    {
        var expectedData = "Test Data";
        var response = new RestResponse<string>(expectedData);

        Assert.AreEqual(expectedData, response.Data);
        Assert.IsNotNull(response.Response);
        Assert.IsNotNull(response.Request);
    }

    [TestMethod]
    public void RestResponse_RequestConstructor_SetsRequest()
    {
        var expectedRequest = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new RestResponse<string>(expectedRequest);

        Assert.AreSame(expectedRequest, response.Request);
        Assert.IsNull(response.BodyString);
        Assert.IsNull(response.Data);
        Assert.IsNotNull(response.Response);
    }

    [TestMethod]
    public void RestResponse_RequestAndBodyStringConstructor_SetsRequestAndBodyString()
    {
        var expectedRequest = new HttpRequestMessage(HttpMethod.Post, "https://example.com");
        var expectedBodyString = "{\"key\":\"value\"}";
        var response = new RestResponse<string>(expectedRequest, expectedBodyString);

        Assert.AreSame(expectedRequest, response.Request);
        Assert.AreEqual(expectedBodyString, response.BodyString);
        Assert.IsNull(response.Data);
        Assert.IsNotNull(response.Response);
    }

    [TestMethod]
    public void ToString_WithData_ReturnsDataToString()
    {
        var expectedData = "Expected String Value";
        var response = new RestResponse<string>(expectedData);

        Assert.AreEqual(expectedData, response.ToString());
    }

    [TestMethod]
    public void ToString_WithNullData_ReturnsEmptyString()
    {
        var response = new RestResponse<string>();

        Assert.AreEqual(string.Empty, response.ToString());
    }

    [TestMethod]
    public void ToString_WithCustomObject_ReturnsObjectToString()
    {
        var data = new CustomTestClass { Value = 42 };
        var response = new RestResponse<CustomTestClass>(data);

        Assert.AreEqual("CustomTestClass: 42", response.ToString());
    }

    private class CustomTestClass
    {
        public int Value { get; set; }

        public override string ToString()
        {
            return $"CustomTestClass: {Value}";
        }
    }
}
