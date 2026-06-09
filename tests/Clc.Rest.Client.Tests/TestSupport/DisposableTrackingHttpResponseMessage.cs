using System.Net;
using System.Net.Http;

namespace Clc.Rest.Client.Tests;

internal sealed class DisposableTrackingHttpResponseMessage(HttpStatusCode statusCode) : HttpResponseMessage(statusCode)
{
    public bool IsDisposed { get; private set; }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
