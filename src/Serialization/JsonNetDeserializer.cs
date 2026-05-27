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
            if (input == null)
            {
                return default!;
            }

            return JsonConvert.DeserializeObject<T>(input)!;
        }
    }
}
