# README

A simple library for making REST requests.

## Target framework

`Clc.Rest.Client` v3 alpha targets **.NET 8+** (`net8.0`) only.

## 3.0.0-alpha.1 / net8-only v3 alpha breaking changes

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

- `QueryParameters` are always appended to the URL for any method.
- `Body` is serialized using `request.Serializer ?? client.Serializer`.
- `Content` bypasses serialization and is used directly.
- `PostForm` is a convenience for `application/x-www-form-urlencoded` content.
- For headers, serializer, authenticator, or request-specific formatting, configure the returned `RestRequest` before calling `ExecuteAsync`.

```csharp
var request = RestRequest.Get("/items", new Dictionary<string, object>
{
    ["page"] = 2,
    ["limit"] = 50,
    ["includeDeleted"] = false
});
await client.ExecuteAsync<ItemSearchResult>(request, token);

var createRequest = RestRequest.Post("/items", body, queryParameters);
createRequest.Headers["X-Test"] = "value";
await client.ExecuteAsync<MyDto>(createRequest, token);

var formRequest = RestRequest.PostForm("/token", formValues);
await client.ExecuteAsync<TokenDto>(formRequest, token);
```

Removed in this alpha:

- URL-only and method/url `ExecuteAsync` convenience overloads (use `RestRequest` factories)
- context-dependent `Parameters` behavior (replaced by `QueryParameters` plus explicit `Content`/`PostForm`)
- async verb helpers (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`)
- legacy `FormatResponse<T>(HttpResponseMessage)` override path
- old `IRestRequest.FormatOutput(HttpResponseMessage)` delegate
- synchronous `Execute<T>(RestRequest)` wrapper
