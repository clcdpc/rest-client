# README

A simple library for making REST requests.

## 3.0.0-alpha.1 breaking changes

Execution uses one async method:

- `ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default)`

Use `RestRequest` factory methods for common shapes:

- `RestRequest.Get(path, queryParameters)`
- `RestRequest.Post(path, body, queryParameters)`
- `RestRequest.Put(path, body, queryParameters)`
- `RestRequest.Patch(path, body, queryParameters)`
- `RestRequest.Delete(path, queryParameters)`
- `RestRequest.PostForm(path, formValues, queryParameters)`
- `RestRequest.WithContent(method, path, content, queryParameters)`
- `RestRequest.Create(method, path, body, queryParameters)`

Request behavior:
- `QueryParameters` are always appended to the URL query string for any method.
- `Body` is serialized with `request.Serializer ?? client.Serializer` when `Content` is null.
- `Content` bypasses body serialization and is sent directly.
- `PostForm` is a convenience for `application/x-www-form-urlencoded` content.

For headers, serializer, authenticator, or request-specific formatting, configure the `RestRequest` before `ExecuteAsync`.

```csharp
var request = RestRequest.Post("/items", body, queryParameters);
request.Headers["X-Test"] = "value";
await client.ExecuteAsync<MyDto>(request, token);

var formRequest = RestRequest.PostForm("/token", formValues);
await client.ExecuteAsync<TokenDto>(formRequest, token);
```

Removed in this alpha:
- URL-only and method/url `ExecuteAsync` convenience overloads were removed in favor of `RestRequest` factories.
- Context-dependent `Parameters` behavior was replaced by `QueryParameters` plus explicit `Content`/`PostForm`.
- sync `Execute<T>` and sync-over-async response construction remain removed.
- legacy formatter paths remain removed in favor of content-aware async formatting.
