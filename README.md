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

`ExecuteAsync<T>` is intended for JSON/text REST APIs with reasonably sized bodies.
It reads response content as text for formatting and deserialization. Diagnostic
request and response body strings are controlled by the client-level
`RestClient.Diagnostics` options so callers do not need to pass diagnostics
settings to each request.

By default:

- `RestRequest.Body` values serialized by the library are captured in
  `IRestResponse.BodyString`.
- Explicit `RestRequest.Content` / `HttpContent` is **not** captured in
  `IRestResponse.BodyString`.
- Response content is captured in `IRestResponse.Response.Content`.

Available options:

- `CaptureSerializedRequestBody` controls diagnostic capture of serialized
  `RestRequest.Body` content.
- `CaptureExplicitRequestContent` controls diagnostic capture of explicit
  caller-supplied `HttpContent`. Enable this only intentionally because explicit
  content can contain credentials, token grant forms, PII, binary data, or
  stream/custom content.
- `CaptureResponseContent` controls whether the already-read response text is
  stored on `HttpResponse.Content`. If this is false, response content is still
  read internally for `ExecuteAsync<T>` formatting and deserialization; it is
  just not stored on `HttpResponse.Content`.
- `MaxCapturedContentLength` limits only stored diagnostic strings such as
  `IRestResponse.BodyString` and `IRestResponse.Response.Content`. It does not
  truncate the actual request sent or the full response string used internally
  for formatting/deserialization.

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
