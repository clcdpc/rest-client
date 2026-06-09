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
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        authenticator.Authenticate(request);

        // Assert
        Assert.IsTrue(request.Headers.Contains("apikey"));
        Assert.AreEqual(apiKey, request.Headers.GetValues("apikey").Single());
    }

    [TestMethod]
    public void Authenticate_WithCustomHeaderName_AddsCustomApiKeyHeader()
    {
        // Arrange
        const string apiKey = "my-secret-key";
        const string customHeaderName = "X-Api-Key";
        var authenticator = new HeaderApiKeyAuthenticator(apiKey, customHeaderName);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        authenticator.Authenticate(request);

        // Assert
        Assert.IsTrue(request.Headers.Contains(customHeaderName));
        Assert.AreEqual(apiKey, request.Headers.GetValues(customHeaderName).Single());
        Assert.IsFalse(request.Headers.Contains("apikey"));
    }

    [TestMethod]
    public void Authenticate_WhenHeaderAlreadyExists_ReplacesExistingHeaderValue()
    {
        // Arrange
        const string apiKey = "my-new-secret-key";
        var authenticator = new HeaderApiKeyAuthenticator(apiKey);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        request.Headers.Add("apikey", "old-key");

        // Act
        authenticator.Authenticate(request);

        // Assert
        var headerValues = request.Headers.GetValues("apikey").ToArray();
        Assert.AreEqual(1, headerValues.Length);
        Assert.AreEqual(apiKey, headerValues.Single());
    }

}
