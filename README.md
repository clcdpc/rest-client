# README

A simple library for making REST requests.

## Async cancellation and error behavior

`ExecuteAsync<T>` overloads support cancellation with `CancellationToken`.

- The token is passed to `HttpClient.SendAsync`.
- For async request/response content reads, cancellation is honored cooperatively around content reads.
- `RestClient` captures exceptions in `IRestResponse.Exception` instead of propagating them, including:
  - `OperationCanceledException` when cancellation occurs (including cancellation before `SendAsync`).
  - `HttpRequestException` from `SendAsync`.
  - deserialization exceptions.

## Custom response formatting guidance

- For new custom formatters, prefer overriding `FormatResponseAsync<T>(HttpResponseMessage response, string content)`.
- `ExecuteAsync<T>` reads HTTP response content once and passes that string into `FormatResponseAsync<T>`.
- Legacy overrides of `FormatResponse<T>(HttpResponseMessage response)` remain supported.
- During `ExecuteAsync<T>`, legacy overrides receive a compatibility `HttpResponseMessage` whose `Content` is backed by the already-read response content string.
- Reading `response.Content` inside a legacy override does not re-read the original HTTP response stream.

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
