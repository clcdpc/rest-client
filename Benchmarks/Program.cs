using System;
using System.Net.Http;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public class PropertyBenchmark
{
    private HttpClient? _client1;
    private HttpClient? _client2;
    private HttpClient? _client3;

    public HttpClient NullCoalescing
    {
        get
        {
            return _client1 ??= new HttpClient();
        }
    }

    public HttpClient LazyClient
    {
        get
        {
            return LazyInitializer.EnsureInitialized(ref _client2, () => new HttpClient());
        }
    }

    public HttpClient InterlockedClient
    {
        get
        {
            if (_client3 == null)
            {
                Interlocked.CompareExchange(ref _client3, new HttpClient(), null);
            }
            return _client3;
        }
    }

    [Benchmark(Baseline = true)]
    public HttpClient TestNullCoalescing()
    {
        return NullCoalescing;
    }

    [Benchmark]
    public HttpClient TestLazy()
    {
        return LazyClient;
    }

    [Benchmark]
    public HttpClient TestInterlocked()
    {
        return InterlockedClient;
    }
}

public class Program
{
    public static void Main()
    {
        var summary = BenchmarkRunner.Run<PropertyBenchmark>();
    }
}
