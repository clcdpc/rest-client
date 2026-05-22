# README

A simple library for making REST requests.

## Async API

The public async execution API consists of exactly these methods:

- `ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default)`
- `ExecuteAsync<T>(string url, CancellationToken cancellationToken = default)`
- `ExecuteAsync<T>(HttpMethod method, string url, CancellationToken cancellationToken = default)`

Use `RestRequest` whenever you need to pass body, parameters, headers, authenticator, serializer, or request-specific formatting.

## Custom response formatting

Client-level formatting extension point:

- Override `FormatResponseAsync<T>(HttpResponseMessage response, string content, CancellationToken cancellationToken = default)`.
- Use the supplied `content` argument; do not read `response.Content`.

Request-level formatting extension point:

- Set `RestRequest.FormatOutputAsync` with signature `Func<HttpResponseMessage, string, CancellationToken, Task<object>>`.
- The delegate receives the already-read response content string.

`ExecuteAsync` reads response content exactly once and passes that same content string to both response metadata and formatter/deserializer paths.

## Async cancellation and error behavior

- The token is passed to `HttpClient.SendAsync`.
- For async request/response content reads, cancellation is honored cooperatively around content reads.
- `RestClient` captures exceptions in `IRestResponse.Exception` instead of propagating them.

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

## Breaking changes

- Removed legacy `FormatResponse<T>(HttpResponseMessage)`.
- Removed public async verb helpers and async body/parameter overload matrices.
- Replaced `RestRequest.FormatOutput` with async content-aware `RestRequest.FormatOutputAsync`.
