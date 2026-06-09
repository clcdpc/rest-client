

namespace Clc.Rest.Client.Tests;

internal sealed class TrackingSerializer : Clc.Rest.ISerializer
{
    public bool WasCalled { get; private set; }
    public string MediaType => "application/json";

    public string Serialize(object body, bool ignoreNullValues = true)
    {
        WasCalled = true;
        return "{}";
    }
}
