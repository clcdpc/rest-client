# README

A simple library for making REST requests.

## Async cancellation and exception behavior

`RestClient` async APIs support overloads that accept a `CancellationToken`.

- The token is passed to `HttpClient.SendAsync`.
- The token is passed when reading request/response content in async flows.
- `ExecuteAsync` captures exceptions into `IRestResponse.Exception` (including `OperationCanceledException`, `HttpRequestException`, and deserialization exceptions) instead of propagating them.

This means canceled requests return a response object with `Exception` set to an `OperationCanceledException`.

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
