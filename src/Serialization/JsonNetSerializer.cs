
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clc.Rest.Models
{
    public class JsonNetSerializer : ISerializer
    {
        private static readonly JsonSerializerSettings _ignoreNullValuesSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        private static readonly JsonSerializerSettings _includeNullValuesSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include };

        public string MediaType { get; set; } = "application/json";

        public string Serialize(object input, bool ignoreNullValues = true)
        {
            return JsonConvert.SerializeObject(input, ignoreNullValues ? _ignoreNullValuesSettings : _includeNullValuesSettings);
        }
    }
}
