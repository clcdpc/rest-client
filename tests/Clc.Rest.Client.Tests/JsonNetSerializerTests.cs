using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class JsonNetSerializerTests
{
    [TestMethod]
    public void MediaType_Has_Correct_Default_Value()
    {
        var serializer = new JsonNetSerializer();
        Assert.AreEqual("application/json", serializer.MediaType);
    }

    [TestMethod]
    public void Serialize_Basic_Object_Returns_Json_String()
    {
        var serializer = new JsonNetSerializer();
        var input = new { Name = "Alice", Age = 30 };

        var result = serializer.Serialize(input);

        Assert.AreEqual("{\"Name\":\"Alice\",\"Age\":30}", result);
    }

    [TestMethod]
    public void Serialize_With_IgnoreNullValues_True_Ignores_Null_Properties()
    {
        var serializer = new JsonNetSerializer();
        var input = new { Name = "Alice", Nickname = (string?)null };

        var result = serializer.Serialize(input, ignoreNullValues: true);

        Assert.AreEqual("{\"Name\":\"Alice\"}", result);
    }

    [TestMethod]
    public void Serialize_With_IgnoreNullValues_False_Includes_Null_Properties()
    {
        var serializer = new JsonNetSerializer();
        var input = new { Name = "Alice", Nickname = (string?)null };

        var result = serializer.Serialize(input, ignoreNullValues: false);

        Assert.AreEqual("{\"Name\":\"Alice\",\"Nickname\":null}", result);
    }
}
