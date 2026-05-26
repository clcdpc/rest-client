using Clc.Rest.Auth;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Clc.Rest.Models
{
    public class RestRequest : IRestRequest
    {
        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        private Dictionary<string, string> _queryParameters = new Dictionary<string, string>();

        public HttpMethod Method { get; set; } = HttpMethod.Get;
        public string Path { get; set; } = string.Empty;
        public object Body { get; set; }
        public HttpContent Content { get; set; }

        public Dictionary<string, string> Headers
        {
            get => _headers;
            set => _headers = value ?? new Dictionary<string, string>();
        }

        public Dictionary<string, string> QueryParameters
        {
            get => _queryParameters;
            set => _queryParameters = value ?? new Dictionary<string, string>();
        }

        public Func<HttpResponseMessage, string, CancellationToken, Task<object>> FormatOutputAsync { get; set; }
        public ISerializer Serializer { get; set; } = null;
        public IAuthenticator Authenticator { get; set; } = null;

        public RestRequest()
        {
        }

        public RestRequest(string path, object body = null, Dictionary<string, string> queryParameters = null)
            : this(HttpMethod.Get, path, body, queryParameters)
        {
        }

        public RestRequest(HttpMethod method, string path, object body = null, Dictionary<string, string> queryParameters = null)
        {
            Method = method ?? HttpMethod.Get;
            Path = path ?? string.Empty;
            QueryParameters = queryParameters;
            Body = body;
        }

        public static RestRequest Get(string path, Dictionary<string, string> queryParameters = null) =>
            new RestRequest(HttpMethod.Get, path, null, queryParameters);

        public static RestRequest Delete(string path, Dictionary<string, string> queryParameters = null) =>
            new RestRequest(HttpMethod.Delete, path, null, queryParameters);

        public static RestRequest Post(string path, object body = null, Dictionary<string, string> queryParameters = null) =>
            new RestRequest(HttpMethod.Post, path, body, queryParameters);

        public static RestRequest Put(string path, object body = null, Dictionary<string, string> queryParameters = null) =>
            new RestRequest(HttpMethod.Put, path, body, queryParameters);

        public static RestRequest Patch(string path, object body = null, Dictionary<string, string> queryParameters = null) =>
            new RestRequest(new HttpMethod("PATCH"), path, body, queryParameters);

        public static RestRequest PostForm(string path, Dictionary<string, string> formValues, Dictionary<string, string> queryParameters = null) =>
            new RestRequest(HttpMethod.Post, path, null, queryParameters)
            {
                Content = new FormUrlEncodedContent(formValues ?? new Dictionary<string, string>())
            };

        public static RestRequest WithContent(HttpMethod method, string path, HttpContent content, Dictionary<string, string> queryParameters = null) =>
            new RestRequest(method, path, null, queryParameters)
            {
                Content = content
            };

        public static RestRequest Create(HttpMethod method, string path, object body = null, Dictionary<string, string> queryParameters = null) =>
            new RestRequest(method, path, body, queryParameters);
    }
}
