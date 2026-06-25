
using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Clc.Rest;
using Clc.Rest.Models;
using System.Net.Http;

namespace Clc.Rest.Client.Benchmarks
{
    [MemoryDiagnoser]
    public class BuildUrlBenchmark
    {
        private DummyRestClient _client1;
        private RestRequest _request1;

        private DummyRestClient _client2;
        private RestRequest _request2;

        private DummyRestClient _client3;
        private RestRequest _request3;

        [GlobalSetup]
        public void Setup()
        {
            _client1 = new DummyRestClient { BaseUrl = "https://api.example.com/", PathPrefix = "/v1/" };
            _request1 = new RestRequest(HttpMethod.Get, "/items/123");

            _client2 = new DummyRestClient { BaseUrl = "https://api.example.com", PathPrefix = "v1" };
            _request2 = new RestRequest(HttpMethod.Get, "items/123");

            _client3 = new DummyRestClient { BaseUrl = "https://api.example.com" };
            _request3 = new RestRequest(HttpMethod.Get, "/items");
        }

        [Benchmark]
        public string BuildUrl_WithAllSegmentsAndSlashes() => _client1.BuildUrl(_request1);

        [Benchmark]
        public string BuildUrl_WithAllSegmentsNoSlashes() => _client2.BuildUrl(_request2);

        [Benchmark]
        public string BuildUrl_WithoutPrefix() => _client3.BuildUrl(_request3);
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BuildUrlBenchmark>();
        }
    }
}
namespace Clc.Rest.Client.Benchmarks
{
    public class DummyRestClient : Clc.Rest.RestClient
    {
    }
}
