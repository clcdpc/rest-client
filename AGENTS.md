# AGENTS.md

## Purpose

This repository is a small .NET REST client library. Changes should preserve public API compatibility, documented HTTP behavior, and `netstandard2.0` library compatibility unless the task explicitly asks for a breaking change.

Agents must treat this file as repository policy when proposing, editing, or reviewing code.

## Project structure

- Library project: `src/Clc.Rest.Client.csproj`
- Test project: `tests/Clc.Rest.Client.Tests/Clc.Rest.Client.Tests.csproj`
- Main implementation: `src/RestClient.cs`
- Public models/interfaces live under `src/Models`, `src/Auth`, and `src/Serialization`
- Public behavior must be reflected in `README.md`

## Target framework compatibility

The library targets `netstandard2.0`.

Do not use APIs unavailable to `netstandard2.0` in the library project unless one of these is true:

1. the project is intentionally multi-targeted;
2. the code is guarded by target-framework conditionals;
3. the task explicitly asks to change supported frameworks.

The test project may target a newer framework, but that does not mean the library can use newer APIs.

Examples:

- OK in library: `HttpClient.SendAsync(request, cancellationToken)`
- Not OK in `netstandard2.0` library code: `HttpContent.ReadAsStringAsync(cancellationToken)` unless guarded or multi-targeted

## Public API compatibility

Preserve existing public APIs unless the task explicitly requests a breaking change.

- Prefer adding overloads over changing existing signatures.
- Do not remove existing overloads.
- Do not reorder parameters on existing public methods.
- Be careful with optional parameters because they affect source compatibility and named-argument callers.
- When adding overloads, make them ergonomic for named-argument callers.

For async APIs in the 3.0 release surface, keep execution async-only and do not add synchronous `Execute<T>` wrappers.

## HTTP request behavior

Do not change request construction semantics without updating tests and README.

Current documented behavior:

- If `Body` is supplied, serialize it and use it as request content.
- For `POST`, if `Body` is `null`, send `Parameters` as `application/x-www-form-urlencoded`.
- For `POST`, if `Body` is not `null`, preserve the serialized body and do not overwrite it with parameters.
- For non-`POST` methods, append parameters to the URL query string.
- Query-string keys and values must be URL-encoded.
- Existing query strings must be preserved.
- Fragments must remain valid when query strings are appended.

Add tests for any change involving `Body`, `Parameters`, URL building, query strings, headers, or authentication.

## Serialization and deserialization behavior

`RestClient` supports injected serializers and deserializers. Do not bypass them.

- Use `request.Serializer ?? Serializer` for request body serialization.
- Use `Deserializer` for response deserialization.
- Preserve special handling for `string` and `bool` responses unless intentionally changing behavior.
- Preserve `PreDeserialize`.
- Preserve `RestRequest.FormatOutputAsync(HttpResponseMessage response, string content, CancellationToken cancellationToken)` behavior.
- Preserve overridden `RestClient.FormatResponseAsync<T>(HttpResponseMessage response, string content, CancellationToken cancellationToken = default)` behavior.
- Formatting code should use the supplied `content` string and should not read `response.Content` directly.
- Add tests when changing serialization, deserialization, formatting, or preprocessing.

## Exception and cancellation behavior

`ExecuteAsync<T>` currently captures exceptions into `IRestResponse<T>.Exception` during async execution.

When changing async behavior:

- Explicitly decide whether exceptions propagate or are captured.
- Apply that decision consistently.
- Cover send failures, cancellation, content-read failures, formatter failures, and deserialization failures.
- Do not document one behavior and implement another.
- Ensure pre-send work is either included in the documented exception model or documented as propagating.

For cancellation support:

- Add overloads rather than breaking callers.
- Pass `CancellationToken` to `HttpClient.SendAsync`.
- Pass tokens to content reads only where the target framework supports it.
- Do not introduce non-`netstandard2.0` APIs without guards or multi-targeting.
- Add tests proving the token reaches the fake `HttpMessageHandler`.
- Add tests for cancellation before send.
- Add tests for cancellation during request or response content handling when supported.

## Documentation requirements

Update `README.md` whenever changing behavior involving:

- public APIs;
- request body handling;
- parameter/query-string handling;
- headers;
- authentication;
- serialization/deserialization;
- async behavior;
- cancellation;
- exception handling;
- package usage examples.

README statements must match implementation and tests.

## Testing requirements

Run `dotnet test`.

If the command cannot be run, state the exact reason in the PR description.

For changes to library code, add or update tests in `tests/Clc.Rest.Client.Tests`.

Tests should verify behavior, not just implementation details. Prefer specific assertions:

- assert exact exception types when behavior documents specific types;
- assert request method, URI, headers, body, and content type where relevant;
- assert query-string encoding and preservation;
- assert serializer/deserializer hooks are used;
- assert fake handlers receive expected requests and tokens;
- assert response content is not consumed more than intended.

## Packaging and versioning

Do not change package metadata, package ID, target framework, package version, release notes, license, or authors unless the task explicitly asks.

If a change is public API-affecting, mention whether package version/release notes should be updated, but do not bump versions unless requested.

## Dependency rules

Avoid adding dependencies.

If adding a dependency is necessary:

- explain why the existing code cannot handle the task;
- ensure the dependency supports the library target framework;
- avoid broad package version ranges unless already established by the project;
- update tests and documentation.

## Style and implementation preferences

- Keep changes small and focused.
- Follow the existing C# style in nearby files.
- Use `ConfigureAwait(false)` in library async code where existing code does so.
- Avoid large rewrites unless the task asks for refactoring.
- Prefer explicit behavior over hidden side effects.
- Preserve extension points and virtual methods.

## Pull request checklist

Before opening or finalizing a PR, verify:

- library still compiles for `netstandard2.0`;
- existing public overloads remain available;
- new overloads do not create ambiguous calls;
- README matches implementation;
- tests cover success and failure paths;
- `dotnet test` was run, or the exact inability to run it is documented;
- no unsupported APIs were introduced;
- no package metadata changed accidentally;
- behavior changes are intentional and documented.
