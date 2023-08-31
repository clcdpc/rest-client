using Clc.Rest.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Clc.Rest
{
    public interface IRestResponse<T>
    {
        /// <summary>
        /// Deserialized data object from response XML
        /// </summary>
        T Data { get; set; }

        /// <summary>
        /// The raw response from the PAPI service
        /// </summary>
        HttpResponse Response { get; set; }

        /// <summary>
        /// The request that was sent to the PAPI service
        /// </summary>
        HttpRequestMessage Request { get; set; }

        Exception Exception { get; set; }

        /// <summary>
        /// Response time, in milliseconds
        /// </summary>
        long ResponseTime { get; set; }
    }
}
