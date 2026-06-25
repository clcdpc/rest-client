using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Clc.Rest.Models;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace BenchmarkApp
{
    [MemoryDiagnoser]
    public class BuildQueryStringBenchmark
    {
        private RestRequest _requestSingle;
        private RestRequest _requestMultiple;
        private RestRequest _requestWithNulls;
        private MethodInfo _buildQueryStringMethod;

        [GlobalSetup]
        public void Setup()
        {
            _requestSingle = RestRequest.Get("/test", new Dictionary<string, object> { { "id", "123" } });
            _requestMultiple = RestRequest.Get("/test", new Dictionary<string, object>
            {
                { "id", "123" },
                { "name", "John Doe" },
                { "age", 30 },
                { "isActive", true },
                { "tags", "a,b,c" }
            });
            _requestWithNulls = RestRequest.Get("/test", new Dictionary<string, object>
            {
                { "id", "123" },
                { "name", null },
                { "age", "" },
                { "isActive", true },
                { "tags", "  " }
            });

            // Since it's private static, use reflection to call it
            _buildQueryStringMethod = typeof(Clc.Rest.RestClient).GetMethod("BuildQueryString", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [Benchmark]
        public string BuildQueryStringSingle()
        {
            return (string)_buildQueryStringMethod.Invoke(null, new object[] { _requestSingle });
        }

        [Benchmark]
        public string BuildQueryStringMultiple()
        {
            return (string)_buildQueryStringMethod.Invoke(null, new object[] { _requestMultiple });
        }

        [Benchmark]
        public string BuildQueryStringWithNulls()
        {
            return (string)_buildQueryStringMethod.Invoke(null, new object[] { _requestWithNulls });
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BuildQueryStringBenchmark>();
        }
    }
}
