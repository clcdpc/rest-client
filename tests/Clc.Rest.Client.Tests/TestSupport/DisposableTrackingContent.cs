using System.Net.Http;
using System.Text;

namespace Clc.Rest.Client.Tests;

internal sealed class DisposableTrackingContent(string content) : StringContent(content, Encoding.UTF8, "application/json")
{
    public bool IsDisposed { get; private set; }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
