namespace Clc.Rest
{
    public interface IDeserializer
    {
        T Deserialize<T>(string input);
    }
}