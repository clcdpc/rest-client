using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class JsonNetSerializerTests
{
    [TestMethod]
    public void MediaType_Defaults_To_ApplicationJson()
    {
        var serializer = new JsonNetSerializer();

        Assert.AreEqual("application/json", serializer.MediaType);
    }

    [TestMethod]
    public void Serialize_IgnoreNullValues_True_Omits_Null_Properties()
    {
        var serializer = new JsonNetSerializer();
        var input = new { Name = "Alice", Optional = (string?)null };

        var json = serializer.Serialize(input, ignoreNullValues: true);

        Assert.AreEqual("{\"Name\":\"Alice\"}", json);
    }

    [TestMethod]
    public void Serialize_IgnoreNullValues_False_Includes_Null_Properties()
    {
        var serializer = new JsonNetSerializer();
        var input = new { Name = "Alice", Optional = (string?)null };

        var json = serializer.Serialize(input, ignoreNullValues: false);

        Assert.AreEqual("{\"Name\":\"Alice\",\"Optional\":null}", json);
    }
}
