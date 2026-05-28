using System.Linq;
using System.Net.Http;
using Clc.Rest.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests.Auth;

[TestClass]
public class HeaderApiKeyAuthenticatorTests
{
    [TestMethod]
    public void Authenticate_WithDefaultHeaderName_AddsApiKeyHeader()
    {
        // Arrange
        const string apiKey = "my-secret-key";
        var authenticator = new HeaderApiKeyAuthenticator(apiKey);
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        var authenticatedRequest = authenticator.Authenticate(client, request);

        // Assert
        Assert.IsTrue(authenticatedRequest.Headers.Contains("apikey"));
        Assert.AreEqual(apiKey, authenticatedRequest.Headers.GetValues("apikey").Single());
    }

    [TestMethod]
    public void Authenticate_WithCustomHeaderName_AddsCustomApiKeyHeader()
    {
        // Arrange
        const string apiKey = "my-secret-key";
        const string headerName = "X-Api-Key";
        var authenticator = new HeaderApiKeyAuthenticator(apiKey, headerName);
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        var authenticatedRequest = authenticator.Authenticate(client, request);

        // Assert
        Assert.IsTrue(authenticatedRequest.Headers.Contains(headerName));
        Assert.AreEqual(apiKey, authenticatedRequest.Headers.GetValues(headerName).Single());
        Assert.IsFalse(authenticatedRequest.Headers.Contains("apikey"));
    }

    [TestMethod]
    public void Authenticate_WhenHeaderAlreadyExists_ReplacesExistingHeaderValue()
    {
        // Arrange
        const string newApiKey = "my-new-secret-key";
        var authenticator = new HeaderApiKeyAuthenticator(newApiKey);
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        request.Headers.Add("apikey", "old-key");

        // Act
        var authenticatedRequest = authenticator.Authenticate(client, request);
        var headerValues = authenticatedRequest.Headers.GetValues("apikey").ToList();

        // Assert
        Assert.AreEqual(1, headerValues.Count);
        Assert.AreEqual(newApiKey, headerValues.Single());
    }

    [TestMethod]
    public void Authenticate_ReturnsSameRequestInstance()
    {
        // Arrange
        var authenticator = new HeaderApiKeyAuthenticator("my-secret-key");
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        var authenticatedRequest = authenticator.Authenticate(client, request);

        // Assert
        Assert.AreSame(request, authenticatedRequest);
    }
}
