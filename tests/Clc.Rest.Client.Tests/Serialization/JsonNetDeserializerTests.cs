using System;
using Clc.Rest.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Clc.Rest.Client.Tests.Serialization;

[TestClass]
public class JsonNetDeserializerTests
{
    private readonly JsonNetDeserializer _deserializer = new JsonNetDeserializer();

    [TestMethod]
    public void Deserialize_ValidJson_ReturnsPopulatedObject()
    {
        // Arrange
        var json = "{\"Id\": 1, \"Name\": \"Test\"}";

        // Act
        var result = _deserializer.Deserialize<TestModel>(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("Test", result.Name);
    }

    [TestMethod]
    public void Deserialize_NullJson_ReturnsNull()
    {
        // Act
        var result = _deserializer.Deserialize<TestModel>(null!);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_EmptyJson_ReturnsNull()
    {
        // Act
        var result = _deserializer.Deserialize<TestModel>("");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_InvalidJson_ThrowsJsonSerializationException()
    {
        // Arrange
        var json = "{\"Id\": 1, \"Name\": \"Test\""; // Missing closing brace

        // Act & Assert
        try
        {
            _deserializer.Deserialize<TestModel>(json);
            Assert.Fail("Expected JsonSerializationException was not thrown.");
        }
        catch (JsonSerializationException)
        {
            // Expected exception
        }
        catch (JsonReaderException)
        {
            // Also acceptable depending on Json.NET version and specific malformation
        }
    }

    [TestMethod]
    public void Deserialize_EmptyObjectJson_ReturnsNewInstance()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = _deserializer.Deserialize<TestModel>(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Id);
        Assert.IsNull(result.Name);
    }

    private class TestModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
