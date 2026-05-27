using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Clc.Rest;
using Clc.Rest.Models;
using System.Globalization;

[MemoryDiagnoser]
public class RestClientBenchmark
{
    private RestRequest _request;
    private MockRestClient _client;

    [GlobalSetup]
    public void Setup()
    {
        _request = new RestRequest(HttpMethod.Get, "/test");
        for (int i = 0; i < 20; i++)
        {
            _request.QueryParameters.Add($"key{i}", $"value{i}");
        }
        _request.QueryParameters.Add("nullKey", null);
        _request.QueryParameters.Add("emptyKey", "");
        _request.QueryParameters.Add("", "val");

        _client = new MockRestClient();
    }

    [Benchmark(Baseline = true)]
    public void AddParametersOriginal()
    {
        var result = _client.RunAddParameters(_request, new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test"));
    }
}

public class MockRestClient : RestClient
{
    public MockRestClient() : base("https://api.example.com") { }

    public HttpRequestMessage RunAddParameters(RestRequest request, HttpRequestMessage httpRequest)
    {
        return AddParameters(request, httpRequest);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<RestClientBenchmark>();
    }
}
