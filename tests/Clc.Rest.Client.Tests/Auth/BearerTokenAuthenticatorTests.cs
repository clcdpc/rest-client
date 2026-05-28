using System.Net.Http;
using System.Net.Http.Headers;
using Clc.Rest.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests.Auth;

[TestClass]
public class BearerTokenAuthenticatorTests
{
    [TestMethod]
    public void Authenticate_AddsBearerAuthorizationHeader()
    {
        // Arrange
        const string token = "test-token";
        var authenticator = new BearerTokenAuthenticator(token);
        using var client = new HttpClient();
        using var request = new HttpRequestMessage();

        // Act
        var result = authenticator.Authenticate(client, request);

        // Assert
        Assert.IsNotNull(result.Headers.Authorization);
        Assert.AreEqual("Bearer", result.Headers.Authorization.Scheme);
        Assert.AreEqual(token, result.Headers.Authorization.Parameter);
    }

    [TestMethod]
    public void Authenticate_WhenAuthorizationHeaderAlreadyExists_ReplacesExistingHeader()
    {
        // Arrange
        const string token = "new-bearer-token";
        var authenticator = new BearerTokenAuthenticator(token);
        using var client = new HttpClient();
        using var request = new HttpRequestMessage();
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "old-value");

        // Act
        authenticator.Authenticate(client, request);

        // Assert
        Assert.IsNotNull(request.Headers.Authorization);
        Assert.AreEqual("Bearer", request.Headers.Authorization.Scheme);
        Assert.AreEqual(token, request.Headers.Authorization.Parameter);
    }

    [TestMethod]
    public void Authenticate_ReturnsSameRequestInstance()
    {
        // Arrange
        var authenticator = new BearerTokenAuthenticator("test-token");
        using var client = new HttpClient();
        using var request = new HttpRequestMessage();

        // Act
        var result = authenticator.Authenticate(client, request);

        // Assert
        Assert.AreSame(request, result);
    }
}
