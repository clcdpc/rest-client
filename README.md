# README

A simple library for making REST requests.

## 3.0.0-alpha.1 breaking changes

Execution uses one async method:

- `ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default)`

Use `RestRequest` factory methods for common request shapes:

- `RestRequest.Get(path, queryParameters)`
- `RestRequest.Post(path, body, queryParameters)`
- `RestRequest.Put(path, body, queryParameters)`
- `RestRequest.Patch(path, body, queryParameters)`
- `RestRequest.Delete(path, queryParameters)`
- `RestRequest.PostForm(path, formValues, queryParameters)`
- `RestRequest.WithContent(method, path, content, queryParameters)`
- `RestRequest.Create(method, path, body, queryParameters)`

Behavior:

- `QueryParameters` are always appended to the URL for any HTTP method.
- `Body` is serialized using `request.Serializer ?? client.Serializer`.
- `Content` bypasses `Body` serialization and is used directly.
- `PostForm` is a convenience helper for `application/x-www-form-urlencoded` request content.

For headers, serializer, authenticator, or request-specific formatting, configure the returned `RestRequest` before passing it to `ExecuteAsync`.

Example:

```csharp
var request = RestRequest.Post("/items", body, queryParameters);
request.Headers["X-Test"] = "value";
await client.ExecuteAsync<MyDto>(request, token);

var formRequest = RestRequest.PostForm("/token", formValues);
await client.ExecuteAsync<TokenDto>(formRequest, token);
```

Removed in this alpha:

- URL-only and method/url `ExecuteAsync` convenience overloads (use `RestRequest` factories instead).
- Context-dependent `Parameters` behavior (replaced by query-only `QueryParameters` and explicit `Content`/`PostForm`).
- async verb helpers (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`)
- async overloads that accept body/query parameters directly outside `RestRequest`
- legacy `FormatResponse<T>(HttpResponseMessage)` override path
- old `IRestRequest.FormatOutput(HttpResponseMessage)` delegate
- synchronous `Execute<T>(RestRequest)` wrapper
