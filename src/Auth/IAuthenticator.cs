using System.Net.Http;

namespace Clc.Rest.Auth
{
    public interface IAuthenticator
    {
        HttpRequestMessage Authenticate(HttpClient client, HttpRequestMessage request);
    }
}
