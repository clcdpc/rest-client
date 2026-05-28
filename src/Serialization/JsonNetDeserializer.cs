using Newtonsoft.Json;

namespace Clc.Rest.Serialization
{
    public class JsonNetDeserializer : IDeserializer
    {
        public T Deserialize<T>(string input)
        {
            return JsonConvert.DeserializeObject<T>(input)!;
        }
    }
}
