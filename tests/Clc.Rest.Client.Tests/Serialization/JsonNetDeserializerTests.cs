using System;
using System.Collections.Generic;
using Clc.Rest.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Clc.Rest.Client.Tests.Serialization;

[TestClass]
public class JsonNetDeserializerTests
{
    private sealed class TestModel
    {
        public int Id { get; set; }

        public bool IsActive { get; set; }

        public string? Name { get; set; }
    }

    [TestMethod]
    public void Deserialize_ValidJson_ReturnsDeserializedObject()
    {
        var deserializer = new JsonNetDeserializer();
        const string json = "{\"Id\":1,\"IsActive\":true,\"Name\":\"Test\"}";

        var result = deserializer.Deserialize<TestModel>(json);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual("Test", result.Name);
    }

    [TestMethod]
    public void Deserialize_EmptyObjectJson_ReturnsNewInstanceWithDefaultValues()
    {
        var deserializer = new JsonNetDeserializer();

        var result = deserializer.Deserialize<TestModel>("{}");

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Id);
        Assert.IsFalse(result.IsActive);
        Assert.IsNull(result.Name);
    }

    [TestMethod]
    public void Deserialize_JsonArray_ReturnsList()
    {
        var deserializer = new JsonNetDeserializer();
        const string json = "[{\"Id\":1,\"Name\":\"One\"},{\"Id\":2,\"Name\":\"Two\"}]";

        var result = deserializer.Deserialize<List<TestModel>>(json);

        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual("One", result[0].Name);
        Assert.AreEqual(2, result[1].Id);
        Assert.AreEqual("Two", result[1].Name);
    }

    [TestMethod]
    public void Deserialize_NullJson_ThrowsArgumentNullException()
    {
        var deserializer = new JsonNetDeserializer();

        ArgumentNullException? exception = null;

        try
        {
            deserializer.Deserialize<TestModel>(null!);
        }
        catch (ArgumentNullException ex)
        {
            exception = ex;
        }

        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        var deserializer = new JsonNetDeserializer();

        var result = deserializer.Deserialize<TestModel>(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_MalformedJson_ThrowsJsonException()
    {
        var deserializer = new JsonNetDeserializer();

        JsonException? exception = null;

        try
        {
            deserializer.Deserialize<TestModel>("{\"Id\":");
        }
        catch (JsonException ex)
        {
            exception = ex;
        }

        Assert.IsNotNull(exception);
    }
}
