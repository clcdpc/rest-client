# README

A simple library for making REST requests.

## Async cancellation and exception behavior

`ExecuteAsync<T>` overloads support an optional `CancellationToken` parameter. The token is passed to:

- `HttpClient.SendAsync(...)`
- async request-content reads
- async response-content reads

When cancellation or any other exception happens during async execution, `RestClient` captures the exception in `IRestResponse<T>.Exception` and returns the response object; exceptions are **not propagated** from `ExecuteAsync<T>`.

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
