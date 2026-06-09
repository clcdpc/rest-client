# README

A simple library for making REST requests.

## Framework support

`Clc.Rest.Client` is currently in beta and targets **.NET 8 (`net8.0`) only**. Consumers must run on .NET 8 or newer.
The beta API may still change, including breaking changes, while the library is being finalized.


## HttpClient lifetime

RestClient manages default HTTP transport internally. If no `HttpClient` is
supplied, RestClient uses a shared process-lifetime `HttpClient` configured for
stateless REST calls. Consumers do not need to dispose a RestClient instance only
to clean up the default client.

Applications that need custom transport behavior can inject an `HttpClient`:

```csharp
services.AddHttpClient<MyApiClient>();
```

Injected `HttpClient` instances are caller-owned. RestClient will use them but
will not dispose them. Derived clients cannot access the internally managed
default `HttpClient`.

Request-specific state such as authorization, API keys, Accept headers, custom
headers, and content should be configured on `RestRequest` or through protected
`HttpRequestMessage` hooks. Transport-level customization such as cookies, a
proxy, custom certificates, custom TLS configuration, handlers, timeout policies,
resilience handlers, or diagnostics requires injecting an appropriately
configured `HttpClient`.

## Diagnostic body capture

`ExecuteAsync<T>` is intended for JSON/text REST APIs with reasonably sized
bodies. It reads response content as text so the same content string can be used
for request-specific formatting, `FormatResponseAsync<T>`, and deserialization.

By default:

- `RestRequest.Body` values serialized by the library are captured in
  `IRestResponse<T>.BodyString`.
- Explicit `RestRequest.Content` / caller-supplied `HttpContent` is sent but is
  not captured in `IRestResponse<T>.BodyString`.
- Response content is captured in `IRestResponse<T>.Response.Content`.

Capture behavior is configured once per client with `RestClient.Diagnostics`:

- `CaptureSerializedRequestBody` controls capture of serialized
  `RestRequest.Body` values.
- `CaptureExplicitRequestContent` controls capture of explicit `HttpContent`.
- `CaptureResponseContent` controls storage of response content on
  `HttpResponse.Content`.
- `MaxCapturedContentLength` limits stored diagnostic request/response strings by
  character count.

Any captured body string may contain sensitive application data. Avoid logging
captured body strings broadly, and consider disabling or truncating capture for
production clients.

Enable `CaptureExplicitRequestContent` only intentionally because explicit
`HttpContent` can contain credentials, token grant forms, PII, binary data, or
stream/custom content. When `CaptureExplicitRequestContent` is enabled, explicit
`HttpContent` is read before the request is sent. Use it only with content that
is safe to buffer/read for diagnostics.

`MaxCapturedContentLength` limits only stored diagnostic strings returned to
callers; it does not truncate the actual request content sent or the full
response string used internally for formatting/deserialization.
`MaxCapturedContentLength` is not a streaming or read-size limit; content is read
before the stored diagnostic string is truncated. If `CaptureResponseContent` is
false, response content is still read internally for `ExecuteAsync<T>`
formatting/deserialization; it is just not stored on `HttpResponse.Content`.

```csharp
var client = new MyApiClient(httpClient);
client.Diagnostics.CaptureExplicitRequestContent = true;
client.Diagnostics.MaxCapturedContentLength = 20_000;
```

## Current beta breaking changes

This beta release continues the v3 API work and may still include breaking changes while the library is being finalized.

Execution uses one async method:

- `ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default)`
- `BuildRequestUri(RestRequest request)` to compute the final URI, including query parameters, without sending the request

Use `RestRequest` factory methods for common request shapes:

- `RestRequest.Get(path, queryParameters)`
- `RestRequest.Post(path, body, queryParameters)`
- `RestRequest.Put(path, body, queryParameters)`
- `RestRequest.Patch(path, body, queryParameters)`
- `RestRequest.Delete(path, queryParameters)`
- `RestRequest.PostForm(path, formValues, queryParameters)`
- `RestRequest.WithContent(method, path, content, queryParameters)`
- `RestRequest.Create(method, path, body, queryParameters)`

Behavior:

- `QueryParameters` are always appended to the URL for any method.
- `BuildRequestUri` applies the same `BaseUrl`, `PathPrefix`, absolute URL, relative URL, existing query string, fragment, and `QueryParameters` rules that `ExecuteAsync` uses before sending. This lets downstream signing or authentication code hash the exact URI rest-client will send.
- `Body` is serialized using `request.Serializer ?? client.Serializer`.
- `Content` bypasses serialization and is used directly.
- `PostForm` is a convenience for `application/x-www-form-urlencoded` content.
- For headers, serializer, authenticator, or request-specific formatting, configure the returned `RestRequest` before calling `ExecuteAsync`. Built-in authenticators apply authentication to the outgoing `HttpRequestMessage` and do not mutate `HttpClient.DefaultRequestHeaders`.

```csharp
var request = RestRequest.Get("/items", new Dictionary<string, object>
{
    ["page"] = 2,
    ["limit"] = 50,
    ["includeDeleted"] = false
});
var finalUri = client.BuildRequestUri(request);
await client.ExecuteAsync<ItemSearchResult>(request, token);

var createRequest = RestRequest.Post("/items", body, queryParameters);
createRequest.Headers["X-Test"] = "value";
await client.ExecuteAsync<MyDto>(createRequest, token);

var formRequest = RestRequest.PostForm("/token", formValues);
await client.ExecuteAsync<TokenDto>(formRequest, token);
```

Removed or changed in this beta:

- URL-only and method/url `ExecuteAsync` convenience overloads (use `RestRequest` factories)
- context-dependent `Parameters` behavior (replaced by `QueryParameters` plus explicit `Content`/`PostForm`)
- async verb helpers (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`)
- legacy `FormatResponse<T>(HttpResponseMessage)` override path
- old `IRestRequest.FormatOutput(HttpResponseMessage)` delegate
- the old `IAuthenticator.Authenticate(HttpClient, HttpRequestMessage)` signature
- `IAuthenticator.Authenticate(HttpRequestMessage)` now returns `void` and mutates the supplied `HttpRequestMessage`
- protected `Client` access from `RestClient`
- request-building hooks that return replacement `HttpRequestMessage` instances; hooks are mutate-only
- unsupported request-message replacement from authenticators or request-building hooks
- `CreateHttpRequestMessage` and `SendAsync` are the supported protected extension points
- synchronous `Execute<T>(RestRequest)` wrapper