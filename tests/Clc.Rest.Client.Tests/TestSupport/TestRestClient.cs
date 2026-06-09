using System.Net.Http;

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
