using System.Net;
using System.Net.Http.Headers;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests.Models;

[TestClass]
public class HttpResponseTests
{
	[TestMethod]
	public void Constructor_WithContent_MapsPropertiesCorrectly()
	{
		var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
		var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
		{
			ReasonPhrase = "All Good",
			RequestMessage = requestMessage,
			Version = new Version(2, 0),
			Content = new StringContent("Hello World")
		};
		responseMessage.Headers.Add("X-Test-Header", new[] { "Value1", "Value2" });
		responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

		var httpResponse = new HttpResponse(responseMessage, "Hello World");

		Assert.AreEqual("Hello World", httpResponse.Content);
		Assert.AreEqual("text/plain", httpResponse.ContentType);
		Assert.IsTrue(httpResponse.Headers.ContainsKey("X-Test-Header"));
		CollectionAssert.AreEqual(new[] { "Value1", "Value2" }, httpResponse.Headers["X-Test-Header"]);
		Assert.IsTrue(httpResponse.ContentHeaders.ContainsKey("Content-Type"));
		CollectionAssert.AreEqual(new[] { "text/plain" }, httpResponse.ContentHeaders["Content-Type"]);
		Assert.IsTrue(httpResponse.IsSuccessStatusCode);
		Assert.AreEqual("All Good", httpResponse.ReasonPhrase);
		Assert.AreSame(requestMessage, httpResponse.RequestMessage);
		Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);
		Assert.AreEqual(new Version(2, 0), httpResponse.Version);
	}

	[TestMethod]
	public void Constructor_WithoutContent_MapsPropertiesCorrectly()
	{
		var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
		var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
		{
			ReasonPhrase = "Not Found",
			RequestMessage = requestMessage,
			Version = new Version(1, 1),
			Content = null
		};
		responseMessage.Headers.Add("X-Test-Header", "Value1");

		var httpResponse = new HttpResponse(responseMessage, null);

		Assert.IsNull(httpResponse.Content);
		Assert.AreEqual("", httpResponse.ContentType);
		Assert.IsTrue(httpResponse.Headers.ContainsKey("X-Test-Header"));
		CollectionAssert.AreEqual(new[] { "Value1" }, httpResponse.Headers["X-Test-Header"]);
		Assert.IsNotNull(httpResponse.ContentHeaders);
		Assert.AreEqual(0, httpResponse.ContentHeaders.Count);
		Assert.IsFalse(httpResponse.IsSuccessStatusCode);
		Assert.AreEqual("Not Found", httpResponse.ReasonPhrase);
		Assert.AreSame(requestMessage, httpResponse.RequestMessage);
		Assert.AreEqual(HttpStatusCode.NotFound, httpResponse.StatusCode);
		Assert.AreEqual(new Version(1, 1), httpResponse.Version);
	}

	[TestMethod]
	public void ToString_ReturnsCorrectlyFormattedString()
	{
		var httpResponse = new HttpResponse
		{
			StatusCode = HttpStatusCode.Created,
			ContentType = "application/json",
			Content = "{\"id\":1}"
		};

		var result = httpResponse.ToString();

		Assert.AreEqual("StatusCode: Created, Content-Type: application/json, Content-Length: 8", result);
	}

	[TestMethod]
	public void ToString_WithNullContent_ReturnsCorrectlyFormattedString()
	{
		var httpResponse = new HttpResponse
		{
			StatusCode = HttpStatusCode.NoContent,
			ContentType = null,
			Content = null
		};

		var result = httpResponse.ToString();

		Assert.AreEqual("StatusCode: NoContent, Content-Type: , Content-Length: 0", result);
	}
}