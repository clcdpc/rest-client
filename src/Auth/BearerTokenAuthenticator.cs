using System.Net.Http;

namespace Clc.Rest.Auth
{
    public class BearerTokenAuthenticator : IAuthenticator
    {
        private string Token { get; }

        public BearerTokenAuthenticator(string token)
        {
            Token = token;
        }

        public HttpRequestMessage Authenticate(HttpRequestMessage request)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $"{Token}");
            return request;
        }
    }
}
