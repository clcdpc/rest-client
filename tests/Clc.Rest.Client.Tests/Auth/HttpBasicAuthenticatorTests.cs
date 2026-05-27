using System;
using System.Net.Http;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Clc.Rest.Auth;

namespace Clc.Rest.Client.Tests.Auth;

[TestClass]
public class HttpBasicAuthenticatorTests
{
    [TestMethod]
    public void Authenticate_Sets_Authorization_Header_Correctly()
    {
        // Arrange
        var username = "testuser";
        var password = "testpassword";
        var authenticator = new HttpBasicAuthenticator(username, password);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var client = new HttpClient();

        // Act
        authenticator.Authenticate(client, request);

        // Assert
        Assert.IsNotNull(request.Headers.Authorization);
        Assert.AreEqual("Basic", request.Headers.Authorization.Scheme);

        var expectedParameter = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        Assert.AreEqual(expectedParameter, request.Headers.Authorization.Parameter);
    }
}
