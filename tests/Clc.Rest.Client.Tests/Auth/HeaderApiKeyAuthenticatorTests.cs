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
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        var authenticatedRequest = authenticator.Authenticate(request);

        // Assert
        Assert.IsTrue(authenticatedRequest.Headers.Contains("apikey"));
        Assert.AreEqual(apiKey, authenticatedRequest.Headers.GetValues("apikey").Single());
        Assert.IsFalse(client.DefaultRequestHeaders.Contains("apikey"));
    }

    [TestMethod]
    public void Authenticate_WithCustomHeaderName_AddsCustomApiKeyHeader()
    {
        // Arrange
        const string apiKey = "my-secret-key";
        const string customHeaderName = "X-Api-Key";
        var authenticator = new HeaderApiKeyAuthenticator(apiKey, customHeaderName);
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        var authenticatedRequest = authenticator.Authenticate(request);

        // Assert
        Assert.IsTrue(authenticatedRequest.Headers.Contains(customHeaderName));
        Assert.AreEqual(apiKey, authenticatedRequest.Headers.GetValues(customHeaderName).Single());
        Assert.IsFalse(authenticatedRequest.Headers.Contains("apikey"));
        Assert.IsFalse(client.DefaultRequestHeaders.Contains(customHeaderName));
    }

    [TestMethod]
    public void Authenticate_WhenHeaderAlreadyExists_ReplacesExistingHeaderValue()
    {
        // Arrange
        const string apiKey = "my-new-secret-key";
        var authenticator = new HeaderApiKeyAuthenticator(apiKey);
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        request.Headers.Add("apikey", "old-key");

        // Act
        var authenticatedRequest = authenticator.Authenticate(request);

        // Assert
        var headerValues = authenticatedRequest.Headers.GetValues("apikey").ToArray();
        Assert.AreEqual(1, headerValues.Length);
        Assert.AreEqual(apiKey, headerValues.Single());
        Assert.IsFalse(client.DefaultRequestHeaders.Contains("apikey"));
    }

    [TestMethod]
    public void Authenticate_ReturnsSameRequestInstance()
    {
        // Arrange
        var authenticator = new HeaderApiKeyAuthenticator("my-secret-key");
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        var authenticatedRequest = authenticator.Authenticate(request);

        // Assert
        Assert.AreSame(request, authenticatedRequest);
        Assert.IsFalse(client.DefaultRequestHeaders.Contains("apikey"));
    }
}
