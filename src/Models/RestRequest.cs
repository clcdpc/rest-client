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
        public HttpMethod Method { get; set; }
        public string Path { get; set; }
        public object Body { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public Func<HttpResponseMessage, object> FormatOutput { get; set; }
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
            Method = method;
            Path = path;
            if (parameters != null) { Parameters = parameters; }
            if (body != null) { Body = body; }
        }
    }
}
