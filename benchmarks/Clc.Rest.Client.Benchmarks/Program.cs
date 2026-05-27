using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Clc.Rest.Models;
using System.Collections.Generic;

namespace Clc.Rest.Client.Benchmarks
{
    [MemoryDiagnoser]
    public class JsonSerializerBenchmark
    {
        private JsonNetSerializer _serializer;
        private object _testObject;

        [GlobalSetup]
        public void Setup()
        {
            _serializer = new JsonNetSerializer();
            _testObject = new { Name = "Test", Value = 123, Items = new List<string> { "a", "b", "c" } };
        }

        [Benchmark]
        public string SerializeWithIgnoreNull() => _serializer.Serialize(_testObject, true);

        [Benchmark]
        public string SerializeWithIncludeNull() => _serializer.Serialize(_testObject, false);
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<JsonSerializerBenchmark>();
        }
    }
}
