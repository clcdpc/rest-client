using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clc.Rest.Serialization
{
    public class JsonNetDeserializer : IDeserializer
    {
        public T Deserialize<T>(string input)
        {
            return JsonConvert.DeserializeObject<T>(input);
        }
    }
}
