using Clc.Rest.Models;
using System;
using System.Net.Http;

namespace Clc.Rest
{
    public interface IRestResponse<T>
    {
        T? Data { get; set; }
        HttpResponse? Response { get; set; }
        HttpRequestMessage? Request { get; set; }
        string? BodyString { get; set; }
        Exception? Exception { get; set; }
        long ResponseTime { get; set; }
    }
}
