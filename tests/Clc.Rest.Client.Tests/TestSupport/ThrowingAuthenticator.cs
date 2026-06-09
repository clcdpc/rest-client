using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

internal sealed class ThrowingAuthenticator(Exception exception) : Clc.Rest.Auth.IAuthenticator
{
    private readonly Exception _exception = exception;

    public void Authenticate(HttpRequestMessage request) => throw _exception;
}
