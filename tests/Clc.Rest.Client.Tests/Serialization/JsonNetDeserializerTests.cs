using System.Collections.Generic;
using Clc.Rest.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Clc.Rest.Client.Tests.Serialization;

[TestClass]
public class JsonNetDeserializerTests
{
    private class TestModel
    {
        public int Id { get; set; }

        public bool IsEnabled { get; set; }

        public string? Name { get; set; }
    }

    [TestMethod]
    public void Deserialize_ValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        var deserializer = new JsonNetDeserializer();
        var json = "{\"Id\":1,\"IsEnabled\":true,\"Name\":\"Test\"}";

        // Act
        var result = deserializer.Deserialize<TestModel>(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.IsTrue(result.IsEnabled);
        Assert.AreEqual("Test", result.Name);
    }

    [TestMethod]
    public void Deserialize_EmptyObjectJson_ReturnsNewInstanceWithDefaultValues()
    {
        // Arrange
        var deserializer = new JsonNetDeserializer();
        var json = "{}";

        // Act
        var result = deserializer.Deserialize<TestModel>(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(default, result.Id);
        Assert.AreEqual(default, result.IsEnabled);
        Assert.IsNull(result.Name);
    }

    [TestMethod]
    public void Deserialize_JsonArray_ReturnsList()
    {
        // Arrange
        var deserializer = new JsonNetDeserializer();
        var json = "[{\"Id\":1,\"IsEnabled\":true,\"Name\":\"One\"},{\"Id\":2,\"IsEnabled\":false,\"Name\":\"Two\"}]";

        // Act
        var result = deserializer.Deserialize<List<TestModel>>(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(1, result[0].Id);
        Assert.IsTrue(result[0].IsEnabled);
        Assert.AreEqual("One", result[0].Name);
        Assert.AreEqual(2, result[1].Id);
        Assert.IsFalse(result[1].IsEnabled);
        Assert.AreEqual("Two", result[1].Name);
    }

    [TestMethod]
    public void Deserialize_NullJson_ThrowsArgumentNullException()
    {
        // Arrange
        var deserializer = new JsonNetDeserializer();

        // Act
        ArgumentNullException? exception = null;
        try
        {
            deserializer.Deserialize<TestModel>(null!);
        }
        catch (ArgumentNullException ex)
        {
            exception = ex;
        }

        // Assert
        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        // Arrange
        var deserializer = new JsonNetDeserializer();

        // Act
        var result = deserializer.Deserialize<TestModel>(string.Empty);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_MalformedJson_ThrowsJsonException()
    {
        // Arrange
        var deserializer = new JsonNetDeserializer();
        var malformedJson = "{\"Id\":1,\"Name\":";

        // Act
        JsonException? exception = null;
        try
        {
            deserializer.Deserialize<TestModel>(malformedJson);
        }
        catch (JsonException ex)
        {
            exception = ex;
        }

        // Assert
        Assert.IsNotNull(exception);
    }
}
