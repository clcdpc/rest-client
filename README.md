# README

A simple library for making REST requests.

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

## Custom response formatting guidance

`RestClient` supports custom response formatting via:

- `FormatResponseAsync<T>(HttpResponseMessage response, string content)` (recommended extension point).
- legacy `FormatResponse<T>(HttpResponseMessage response)` overrides (compatibility path).
- per-request `RestRequest.FormatOutput`.

Implementation notes:

- `ExecuteAsync<T>` reads HTTP response content once for the internal pipeline.
- when a legacy `FormatResponse<T>(HttpResponseMessage)` override is present, the client passes a compatibility `HttpResponseMessage` that contains the already-buffered content so legacy formatters can still read `response.Content` without re-reading the original transport stream.
- for new custom formatters, prefer overriding `FormatResponseAsync<T>(HttpResponseMessage response, string content)` and using the provided `content` argument.
