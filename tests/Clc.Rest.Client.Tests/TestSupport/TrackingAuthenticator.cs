using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

internal sealed class TrackingAuthenticator : Clc.Rest.Auth.IAuthenticator
{
    public bool WasCalled { get; private set; }

    public void Authenticate(HttpRequestMessage request)
    {
        WasCalled = true;
    }
}
