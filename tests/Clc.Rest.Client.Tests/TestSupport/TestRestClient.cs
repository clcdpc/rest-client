using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

internal sealed class TestRestClient : Clc.Rest.RestClient
{
    public TestRestClient()
    {
    }

    public TestRestClient(HttpClient client) : base(client)
    {
    }
}
