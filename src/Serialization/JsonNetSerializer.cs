
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clc.Rest.Models
{
    public class JsonNetSerializer : ISerializer
    {
        public string MediaType { get; set; } = "application/json";

        public string Serialize(object input, bool ignoreNullValues = true)
        {
            return JsonConvert.SerializeObject(input, new JsonSerializerSettings { NullValueHandling = ignoreNullValues ? NullValueHandling.Ignore : NullValueHandling.Include });
        }
    }
}
