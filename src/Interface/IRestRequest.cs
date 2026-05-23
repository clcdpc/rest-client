using Clc.Rest.Auth;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clc.Rest
{
    public interface IRestRequest
    {
        HttpMethod Method { get; set; }
        string Path { get; set; }
        object Body { get; set; }
        Func<HttpResponseMessage, object> FormatOutput { get; set; }
        Func<HttpResponseMessage, string, CancellationToken, Task<object>> FormatOutputAsync { get; set; }
        Dictionary<string, string> Headers { get; set; }
        Dictionary<string, string> Parameters { get; set; }
        ISerializer Serializer { get; set; }
        IAuthenticator Authenticator { get; set; }
    }
}