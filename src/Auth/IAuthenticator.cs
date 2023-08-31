using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Clc.Rest.Auth
{
    public interface IAuthenticator
    {
        HttpRequestMessage Authenticate(HttpClient client, HttpRequestMessage request);
    }    
}
