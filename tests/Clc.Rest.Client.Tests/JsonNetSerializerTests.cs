using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class JsonNetSerializerTests
{
    private sealed class Payload
    {
        public string? Name { get; set; }

        public int? Age { get; set; }
    }

    [TestMethod]
    public void Serialize_WithIgnoreNullValuesTrue_OmitsNullProperties()
    {
        var serializer = new JsonNetSerializer();

        var json = serializer.Serialize(new Payload { Name = "Alice", Age = null }, ignoreNullValues: true);

        Assert.AreEqual("{\"Name\":\"Alice\"}", json);
    }

    [TestMethod]
    public void Serialize_WithIgnoreNullValuesFalse_IncludesNullProperties()
    {
        var serializer = new JsonNetSerializer();

        var json = serializer.Serialize(new Payload { Name = "Alice", Age = null }, ignoreNullValues: false);

        Assert.AreEqual("{\"Name\":\"Alice\",\"Age\":null}", json);
    }

    [TestMethod]
    public void MediaType_Default_Is_ApplicationJson()
    {
        var serializer = new JsonNetSerializer();

        Assert.AreEqual("application/json", serializer.MediaType);
    }
}
