# README

A simple library for making REST requests.

## Async API (breaking change)

The public async API is intentionally simplified to exactly these methods:

- `ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default)`
- `ExecuteAsync<T>(string url, CancellationToken cancellationToken = default)` (GET convenience)
- `ExecuteAsync<T>(HttpMethod method, string url, CancellationToken cancellationToken = default)` (method/url convenience)

For body, parameters, headers, authenticators, serializers, or request-specific formatting, construct a `RestRequest` and call the canonical overload.

## Async cancellation and error behavior

- The token is passed to `HttpClient.SendAsync`.
- `RestClient` captures exceptions in `IRestResponse.Exception` instead of propagating them, including cancellation and deserialization failures.

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
- fragments remain valid when query strings are appended.

## Response formatting (breaking change)

`ExecuteAsync` reads response content once and passes the already-read string to formatters.

Client-level formatter extension point:

- `FormatResponseAsync<T>(HttpResponseMessage response, string content, CancellationToken cancellationToken = default)`

Rules:

- use the `content` argument
- do not read `response.Content`
- `PreDeserialize(content)` is applied before deserialization

Request-level formatter extension point:

- `RestRequest.FormatOutputAsync` of type `Func<HttpResponseMessage, string, CancellationToken, Task<object>>`

## Breaking changes summary

- Removed legacy `FormatResponse<T>(HttpResponseMessage)`.
- Removed legacy async verb helpers (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`) and async body/parameter overload matrices.
- Removed old `FormatOutput` delegate in favor of `FormatOutputAsync` with content string and cancellation token.
