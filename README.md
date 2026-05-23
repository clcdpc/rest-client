# README

A simple library for making REST requests.

## 3.0.0-alpha.1 breaking changes

This alpha intentionally simplifies the async API and removes legacy compatibility extension points.

The public async request API is now:

- `ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default)`
- `ExecuteAsync<T>(string url, CancellationToken cancellationToken = default)`
- `ExecuteAsync<T>(HttpMethod method, string url, CancellationToken cancellationToken = default)`

Async calls that need body, parameters, headers, serializer, authenticator, or per-request formatting should construct a `RestRequest`.

Removed in this alpha:

- async verb helpers (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`)
- async overloads that accept body/parameters directly outside `RestRequest`
- legacy `FormatResponse<T>(HttpResponseMessage)` override path
- old `IRestRequest.FormatOutput(HttpResponseMessage)` delegate

Request-specific custom formatting now uses:

- `RestRequest.FormatOutputAsync(HttpResponseMessage response, string content, CancellationToken cancellationToken)`

Client-level custom formatting should override:

- `FormatResponseAsync<T>(HttpResponseMessage response, string content, CancellationToken cancellationToken = default)`

Formatting code should use the supplied `content` string and should not read `response.Content`.
`ExecuteAsync` reads response content once and passes the already-read string to formatting hooks.

### Migration examples

Before (removed async body/parameter overload style):

```csharp
await client.ExecuteAsync<MyDto>(
    "/items",
    HttpMethod.Post,
    parameters: parameters,
    body: body,
    cancellationToken: token);
```

After (construct `RestRequest`):

```csharp
await client.ExecuteAsync<MyDto>(
    new RestRequest(
        HttpMethod.Post,
        "/items",
        body,
        parameters),
    token);
```

Before (legacy formatter override path removed):

```csharp
public override T FormatResponse<T>(HttpResponseMessage response)
{
    // old formatter read response.Content directly
}
```

After (content-aware async formatter):

```csharp
public override Task<T> FormatResponseAsync<T>(
    HttpResponseMessage response,
    string content,
    CancellationToken cancellationToken = default)
{
    // use content instead of reading response.Content
    throw new NotImplementedException();
}
```

## Async cancellation and error behavior

`ExecuteAsync<T>` overloads accept an optional `CancellationToken cancellationToken = default`.

- The token is passed to `HttpClient.SendAsync`.
- For async request/response content reads, cancellation is honored cooperatively around content reads.
- `RestClient` captures exceptions in `IRestResponse.Exception` instead of propagating them, including:
  - `OperationCanceledException` when cancellation occurs (including cancellation before `SendAsync`).
  - `HttpRequestException` from `SendAsync`.
  - deserialization exceptions.

## Request `Body` and `Parameters` behavior

`RestClient` applies `Body` and `Parameters` according to HTTP method:

- If `Body` is supplied, `Body` is serialized and used as request content.
- For `POST` requests:
  - if `Body` is `null`, `Parameters` are sent as `application/x-www-form-urlencoded` content.
  - if `Body` is not `null`, the serialized `Body` is preserved and `Parameters` do not overwrite request content.
- For non-`POST` requests (`GET`, `PUT`, `PATCH`, `DELETE`), `Parameters` are appended to the URL query string.

### Query-string construction details

When `Parameters` are appended to the URL query string:

- both keys and values are URL-encoded.
- existing query strings are preserved.
- the client appends new parameters with `?` or `&` as appropriate.
