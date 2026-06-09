using System.Net.Http;

namespace Clc.Rest.Client.Tests;

internal sealed class InspectingAuthenticator : Clc.Rest.Auth.IAuthenticator
{
    public string? Uri { get; private set; }
    public string? HeaderValue { get; private set; }
    public bool SawContent { get; private set; }
    public string? ContentType { get; private set; }

    public void Authenticate(HttpRequestMessage request)
    {
        Uri = request.RequestUri?.IsAbsoluteUri == true
            ? request.RequestUri.AbsoluteUri
            : request.RequestUri?.OriginalString;

        HeaderValue = request.Headers.TryGetValues("X-Test", out var values)
            ? values.SingleOrDefault()
            : null;

        SawContent = request.Content != null;
        ContentType = request.Content?.Headers.ContentType?.ToString();
    }
}
