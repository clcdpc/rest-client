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
- `Content` bypasses body serialization and is used directly.
- `PostForm` is a convenience for `application/x-www-form-urlencoded` content.
- For request-specific headers/serializer/authenticator/formatting, configure the request before calling `ExecuteAsync`.

```csharp
var request = RestRequest.Post("/items", body, queryParameters);
request.Headers["X-Test"] = "value";
await client.ExecuteAsync<MyDto>(request, token);

var formRequest = RestRequest.PostForm("/token", formValues);
await client.ExecuteAsync<TokenDto>(formRequest, token);
```

Removed in this alpha:

- URL-only and method/url `ExecuteAsync` convenience overloads (use `RestRequest` factories).
- context-dependent `Parameters` behavior (replaced by `QueryParameters` + explicit `Content`/`PostForm`).
- async verb helpers (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`).
- synchronous `Execute<T>(RestRequest)` wrapper.
- legacy `FormatResponse<T>(HttpResponseMessage)` override path.
- old `IRestRequest.FormatOutput(HttpResponseMessage)` delegate.
