
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Clc.Rest.Auth
{
    public class BearerTokenAuthenticator : IAuthenticator
    {
        private string Token { get; }

        public BearerTokenAuthenticator(string token)
        {
            Token = token;
        }

        public HttpRequestMessage Authenticate(HttpClient client, HttpRequestMessage request)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $"{Token}");
            return request;
        }
    }
}
