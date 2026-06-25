1.  **Analyze current code**: The `BuildUrl` method in `src/RestClient.cs` concatenates strings using an array creation and a LINQ `.Where` clause. This results in multiple allocations (the array, string manipulation, LINQ enumerators/closures).
2.  **Optimize `BuildUrl`**:
    *   Since there are only 3 potential segments (`BaseUrl`, `PathPrefix`, and `path`), we can use `string.Concat` or a `StringBuilder` (or a `ref struct` string builder/Span) to concatenate them without array allocation and LINQ.
    *   Instead of allocating an array and enumerating over it, we can create the result string manually. We should check if the segments are null or whitespace, and add the necessary separators.
    *   Also, remember to apply `.TrimEnd('/')`, `.Trim('/')`, and `.TrimStart('/')` to avoid double slashes, but preferably without allocating intermediate trimmed strings if possible, or only doing so if they are not empty. A simple implementation using basic conditional concatenation avoids array and LINQ allocations.
    *   Let's replace `segments = new[] {...}; return string.Join("/", segments.Where(segment => !string.IsNullOrWhiteSpace(segment)));` with a custom appending logic.
3.  **Run tests**: Make sure all existing unit tests in `tests/Clc.Rest.Client.Tests/RestClient/RestClientRequestUriTests.cs` (and others) pass.
4.  **Run benchmark**: Re-run `Clc.Rest.Client.Benchmarks` to verify the improvement. Expect to see zero or reduced allocation from removing the array and LINQ allocations.
5.  **Submit PR**: Follow the pre-commit instructions, run necessary test and validation, then use the `submit` tool to create a PR summarizing the performance gains.
