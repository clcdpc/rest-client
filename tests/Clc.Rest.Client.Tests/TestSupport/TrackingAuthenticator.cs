using System.Net.Http;

namespace Clc.Rest.Client.Tests;

internal sealed class TrackingAuthenticator : Clc.Rest.Auth.IAuthenticator
{
    public bool WasCalled { get; private set; }

    public void Authenticate(HttpRequestMessage request)
    {
        WasCalled = true;
    }
}
