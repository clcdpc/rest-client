# README

A simple library for making REST requests.

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

## Async cancellation and exception behavior

`ExecuteAsync<T>` overloads include token-aware variants that accept a `CancellationToken` and pass it to:

- `HttpClient.SendAsync`
- request content reads
- response content reads

Behavior is consistent for network, cancellation, and deserialization failures: exceptions are captured in `IRestResponse<T>.Exception` and returned to the caller (they are not rethrown from `ExecuteAsync<T>`).
