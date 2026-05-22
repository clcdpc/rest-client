# README

A simple library for making REST requests.

## Async API (breaking change)

`RestClient` now exposes exactly three public async execution methods:

- `ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default)`
- `ExecuteAsync<T>(string url, CancellationToken cancellationToken = default)`
- `ExecuteAsync<T>(HttpMethod method, string url, CancellationToken cancellationToken = default)`

Use `RestRequest` when you need request body, parameters, headers, authenticator, serializer, or request-specific formatting.

## Async cancellation and error behavior

- The token is passed to `HttpClient.SendAsync`.
- For async request/response content reads, cancellation is honored cooperatively around content reads.
- `RestClient` captures exceptions in `IRestResponse.Exception` instead of propagating them, including:
  - `OperationCanceledException` when cancellation occurs (including cancellation before `SendAsync`).
  - `HttpRequestException` from `SendAsync`.
  - deserialization exceptions.

## Response formatting (breaking change)

- Legacy `FormatResponse<T>(HttpResponseMessage response)` was removed.
- Override `FormatResponseAsync<T>(HttpResponseMessage response, string content, CancellationToken cancellationToken = default)` for client-level formatting.
- `FormatResponseAsync` uses the already-read `content` argument and should not read `response.Content`.
- Request-level formatting uses `RestRequest.FormatOutputAsync` with signature `Func<HttpResponseMessage, string, CancellationToken, Task<object>>`.
- `ExecuteAsync` reads response content once and passes that same string to formatters.

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
