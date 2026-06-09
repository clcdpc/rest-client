using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Clc.Rest.Models;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

internal sealed class ThrowOnSecondReadContent : HttpContent
{
    private readonly byte[] _payloadBytes;

    public ThrowOnSecondReadContent(string body)
    {
        _payloadBytes = Encoding.UTF8.GetBytes(body);
        Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
        {
            CharSet = Encoding.UTF8.WebName
        };
    }

    public int ReadCount { get; private set; }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        ReadCount++;
        if (ReadCount > 1)
        {
            throw new InvalidOperationException("Content stream was read more than once.");
        }

        return stream.WriteAsync(_payloadBytes, 0, _payloadBytes.Length);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _payloadBytes.Length;
        return true;
    }
}
