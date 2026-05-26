using Clc.Rest.Auth;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clc.Rest.Models
{
    public class RestRequest : IRestRequest
    {
        public static RestRequest Get(string path, Dictionary<string, string> parameters = null) =>
            new RestRequest(HttpMethod.Get, path, null, parameters);

        public static RestRequest Delete(string path, Dictionary<string, string> parameters = null) =>
            new RestRequest(HttpMethod.Delete, path, null, parameters);

        public static RestRequest Post(string path, object body = null, Dictionary<string, string> parameters = null) =>
            new RestRequest(HttpMethod.Post, path, body, parameters);

        public static RestRequest Put(string path, object body = null, Dictionary<string, string> parameters = null) =>
            new RestRequest(HttpMethod.Put, path, body, parameters);

        public static RestRequest Patch(string path, object body = null, Dictionary<string, string> parameters = null) =>
            new RestRequest(new HttpMethod("PATCH"), path, body, parameters);

        public static RestRequest Create(HttpMethod method, string path, object body = null, Dictionary<string, string> parameters = null) =>
            new RestRequest(method, path, body, parameters);

        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        private Dictionary<string, string> _parameters = new Dictionary<string, string>();

        public HttpMethod Method { get; set; } = HttpMethod.Get;
        public string Path { get; set; } = string.Empty;
        public object Body { get; set; }

        public Dictionary<string, string> Headers
        {
            get => _headers;
            set => _headers = value ?? new Dictionary<string, string>();
        }

        public Dictionary<string, string> Parameters
        {
            get => _parameters;
            set => _parameters = value ?? new Dictionary<string, string>();
        }
        public Func<HttpResponseMessage, string, CancellationToken, Task<object>> FormatOutputAsync { get; set; }
        public ISerializer Serializer { get; set; } = null;
        public IAuthenticator Authenticator { get; set; } = null;

        public RestRequest()
        {

        }

        public RestRequest(string path, object body = null, Dictionary<string, string> parameters = null) : this(HttpMethod.Get, path, body, parameters)
        {

        }

        public RestRequest(HttpMethod method, string path, object body = null, Dictionary<string, string> parameters = null)
        {
            Method = method ?? HttpMethod.Get;
            Path = path ?? string.Empty;
            Parameters = parameters;
            Body = body;
        }
    }
}
