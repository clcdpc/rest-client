using System.Net.Http;
using System.Net.Http.Headers;

namespace Clc.Rest.Auth
{
    public class BearerTokenAuthenticator : IAuthenticator
    {
        private string Token { get; }

        public BearerTokenAuthenticator(string token)
        {
            Token = token;
        }

        public void Authenticate(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", $"{Token}");
        }
    }
}
