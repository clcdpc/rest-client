using Microsoft.VisualStudio.TestTools.UnitTesting;
using Clc.Rest.Serialization;
using System.Collections.Generic;

namespace Clc.Rest.Client.Tests.Serialization;

[TestClass]
public class JsonNetDeserializerTests
{
    private class TestModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [TestMethod]
    public void Deserialize_ValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        var deserializer = new JsonNetDeserializer();
        string json = "{\"Id\": 1, \"Name\": \"Test\"}";

        // Act
        var result = deserializer.Deserialize<TestModel>(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("Test", result.Name);
    }

    [TestMethod]
    public void Deserialize_EmptyJson_ReturnsEmptyObject()
    {
        // Arrange
        var deserializer = new JsonNetDeserializer();
        string json = "{}";

        // Act
        var result = deserializer.Deserialize<TestModel>(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Id);
        Assert.IsNull(result.Name);
    }

    [TestMethod]
    public void Deserialize_NullJson_ThrowsArgumentNullException()
    {
        // Arrange
        var deserializer = new JsonNetDeserializer();

        // Act
        System.ArgumentNullException? exception = null;
        try
        {
            deserializer.Deserialize<TestModel>(null!);
        }
        catch (System.ArgumentNullException ex)
        {
            exception = ex;
        }

        // Assert
        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void Deserialize_JsonArray_ReturnsList()
    {
        // Arrange
        var deserializer = new JsonNetDeserializer();
        string json = "[{\"Id\": 1, \"Name\": \"One\"}, {\"Id\": 2, \"Name\": \"Two\"}]";

        // Act
        var result = deserializer.Deserialize<List<TestModel>>(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual("One", result[0].Name);
        Assert.AreEqual(2, result[1].Id);
        Assert.AreEqual("Two", result[1].Name);
    }
}
