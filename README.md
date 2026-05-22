# README

A simple library for making REST requests.

## Async API (breaking change)

`RestClient` now exposes exactly three public async execution methods:

- `ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default)`
- `ExecuteAsync<T>(string url, CancellationToken cancellationToken = default)`
- `ExecuteAsync<T>(HttpMethod method, string url, CancellationToken cancellationToken = default)`

For body, parameters, headers, authenticators, serializers, or request-specific formatting, construct a `RestRequest` and call `ExecuteAsync<T>(RestRequest, CancellationToken)`.

## Response formatting (breaking change)

The only client-level formatter extension point is:

- `FormatResponseAsync<T>(HttpResponseMessage response, string content, CancellationToken cancellationToken = default)`

`ExecuteAsync` reads response content once and passes the already-read `content` string to all formatting paths.

Request-specific formatting uses:

- `RestRequest.FormatOutputAsync` (`Func<HttpResponseMessage, string, CancellationToken, Task<object>>`)

Formatters should use the supplied content argument and should not read `response.Content`.

Removed APIs:

- `FormatResponse<T>(HttpResponseMessage)`
- async verb helpers (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`)
- old async execution overload matrices with direct body/parameter arguments
- old `RestRequest.FormatOutput` delegate

## Async cancellation and error behavior

- The token is passed to `HttpClient.SendAsync`.
- `RestClient` captures exceptions in `IRestResponse.Exception` instead of propagating them, including `OperationCanceledException`, `HttpRequestException`, and deserialization exceptions.

## Request `Body` and `Parameters` behavior

- If `Body` is supplied, `Body` is serialized and used as request content.
- For `POST` requests:
  - if `Body` is `null`, `Parameters` are sent as `application/x-www-form-urlencoded` content.
  - if `Body` is not `null`, the serialized `Body` is preserved and `Parameters` do not overwrite request content.
- For non-`POST` requests (`GET`, `PUT`, `PATCH`, `DELETE`), `Parameters` are appended to the URL query string.
- Query-string keys and values are URL-encoded, existing query strings are preserved, and fragments remain valid when parameters are appended.

## Migration examples

- Old async body/parameter calls now use `RestRequest`:
  - before: `ExecuteAsync<T>("/data", HttpMethod.Post, parameters, body)`
  - after: `ExecuteAsync<T>(new RestRequest(HttpMethod.Post, "/data", body, parameters), token)`
- Old `FormatResponse` override becomes `FormatResponseAsync` override with `content` argument.
- Old `FormatOutput` delegate becomes `FormatOutputAsync` and receives `(response, content, cancellationToken)`.
