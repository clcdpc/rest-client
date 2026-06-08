using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Clc.Rest.Auth
{
    public class HttpBasicAuthenticator : IAuthenticator
    {
        private string Username { get; }
        private string Password { get; }

        public HttpBasicAuthenticator(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public HttpRequestMessage Authenticate(HttpRequestMessage request)
        {
            var byteArray = Encoding.UTF8.GetBytes($"{Username}:{Password}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            return request;
        }
    }
}
