using System.Net.Http;

namespace Clc.Rest.Auth
{
    public class HeaderApiKeyAuthenticator : IAuthenticator
    {
        public string ApiKey { get; set; }
        public string HeaderName { get; set; }

        public HeaderApiKeyAuthenticator(string apiKey, string headerName = "apikey")
        {
            ApiKey = apiKey;
            HeaderName = headerName;
        }

        public HttpRequestMessage Authenticate(HttpRequestMessage request)
        {
            if (request.Headers.Contains(HeaderName)) { request.Headers.Remove(HeaderName); }
            request.Headers.Add(HeaderName, ApiKey);
            return request;
        }
    }
}
