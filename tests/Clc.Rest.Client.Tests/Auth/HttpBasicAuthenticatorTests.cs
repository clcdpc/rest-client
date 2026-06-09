using System;
using System.Net.Http;
using System.Text;
using Clc.Rest.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests.Auth;

[TestClass]
public class HttpBasicAuthenticatorTests
{
    [TestMethod]
    public void Authenticate_UsesUtf8Encoding()
    {
        // Arrange
        string username = "user_name_with_🚀";
        string password = "password_with_€";
        var authenticator = new HttpBasicAuthenticator(username, password);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        authenticator.Authenticate(request);

        // Assert
        Assert.IsNotNull(request.Headers.Authorization);
        Assert.AreEqual("Basic", request.Headers.Authorization.Scheme);

        string expectedParameter = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        Assert.AreEqual(expectedParameter, request.Headers.Authorization.Parameter);
    }

    [TestMethod]
    public void Authenticate_WithAsciiCredentials_UsesExpectedBasicHeader()
    {
        // Arrange
        string username = "testuser";
        string password = "testpassword";
        var authenticator = new HttpBasicAuthenticator(username, password);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

        // Act
        authenticator.Authenticate(request);

        // Assert
        Assert.IsNotNull(request.Headers.Authorization);
        Assert.AreEqual("Basic", request.Headers.Authorization.Scheme);

        string expectedParameter = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        Assert.AreEqual(expectedParameter, request.Headers.Authorization.Parameter);
    }

}
