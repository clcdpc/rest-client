using System.Net.Http;
using Clc.Rest.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class BearerTokenAuthenticatorTests
{
    [TestMethod]
    public void Authenticate_Adds_Authorization_Header_With_Bearer_Scheme_And_Token()
    {
        // Arrange
        var token = "my-secret-token";
        var authenticator = new BearerTokenAuthenticator(token);
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

        // Act
        var result = authenticator.Authenticate(client, request);

        // Assert
        Assert.AreSame(request, result);
        Assert.IsNotNull(result.Headers.Authorization);
        Assert.AreEqual("Bearer", result.Headers.Authorization.Scheme);
        Assert.AreEqual(token, result.Headers.Authorization.Parameter);
    }
}
