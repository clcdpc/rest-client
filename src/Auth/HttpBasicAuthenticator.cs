
using System;
using System.Collections.Generic;
using System.Net.Http;
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

        public HttpRequestMessage Authenticate(HttpClient client, HttpRequestMessage request)
        {
            var byteArray = Encoding.ASCII.GetBytes($"{Username}:{Password}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            return request;
        }
    }
}
