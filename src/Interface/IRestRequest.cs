using Clc.Rest.Auth;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Clc.Rest
{
    public interface IRestRequest
    {
        HttpMethod Method { get; set; }
        string Path { get; set; }
        object? Body { get; set; }
        Func<HttpResponseMessage, string?, CancellationToken, Task<object?>>? FormatOutputAsync { get; set; }
        Dictionary<string, string> Headers { get; set; }
        Dictionary<string, object> QueryParameters { get; set; }
        HttpContent? Content { get; set; }
        ISerializer? Serializer { get; set; }
        IAuthenticator? Authenticator { get; set; }
    }
}
