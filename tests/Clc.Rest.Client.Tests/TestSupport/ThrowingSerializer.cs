

namespace Clc.Rest.Client.Tests;

internal sealed class ThrowingSerializer(Exception exception) : Clc.Rest.ISerializer
{
    private readonly Exception _exception = exception;
    public string MediaType => "application/json";

    public string Serialize(object body, bool ignoreNullValues = true) => throw _exception;
}
