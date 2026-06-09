using System.Net.Http;

namespace Clc.Rest.Client.Tests;

internal sealed class ThrowingAuthenticator(Exception exception) : Clc.Rest.Auth.IAuthenticator
{
    private readonly Exception _exception = exception;

    public void Authenticate(HttpRequestMessage request) => throw _exception;
}
