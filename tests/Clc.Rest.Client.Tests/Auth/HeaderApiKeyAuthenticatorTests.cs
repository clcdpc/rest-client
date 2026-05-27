using System.Linq;
using System.Net.Http;
using Clc.Rest.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests.Auth;

[TestClass]
public class HeaderApiKeyAuthenticatorTests
{
    [TestMethod]
    public void Authenticate_AddsHeaderWithDefaultName()
    {
        // Arrange
        var authenticator = new HeaderApiKeyAuthenticator("my-secret-key");
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        var authenticatedRequest = authenticator.Authenticate(client, request);

        // Assert
        Assert.IsTrue(authenticatedRequest.Headers.Contains("apikey"));
        Assert.AreEqual("my-secret-key", authenticatedRequest.Headers.GetValues("apikey").Single());
    }

    [TestMethod]
    public void Authenticate_AddsHeaderWithCustomName()
    {
        // Arrange
        var authenticator = new HeaderApiKeyAuthenticator("my-secret-key", "X-Custom-Auth");
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        var authenticatedRequest = authenticator.Authenticate(client, request);

        // Assert
        Assert.IsTrue(authenticatedRequest.Headers.Contains("X-Custom-Auth"));
        Assert.AreEqual("my-secret-key", authenticatedRequest.Headers.GetValues("X-Custom-Auth").Single());
    }

    [TestMethod]
    public void Authenticate_OverwritesExistingHeader()
    {
        // Arrange
        var authenticator = new HeaderApiKeyAuthenticator("my-new-secret-key");
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        request.Headers.Add("apikey", "old-key");

        // Act
        var authenticatedRequest = authenticator.Authenticate(client, request);

        // Assert
        Assert.IsTrue(authenticatedRequest.Headers.Contains("apikey"));
        Assert.AreEqual("my-new-secret-key", authenticatedRequest.Headers.GetValues("apikey").Single());
    }
}
