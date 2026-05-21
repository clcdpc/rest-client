using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Clc.Rest.Models
{
    public class RestResponse<T> : IRestResponse<T>
    {
        /// <summary>
        /// Deserialized data object from response XML
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// The raw response from the PAPI service
        /// </summary>
        public HttpResponse Response { get; set; } = new HttpResponse();

        /// <summary>
        /// The request that was sent to the PAPI service
        /// </summary>
        public HttpRequestMessage Request { get; set; } = new HttpRequestMessage();
        public string BodyString { get; set; }

        public Exception Exception { get; set; }

        /// <summary>
        /// Response time, in milliseconds
        /// </summary>
        public long ResponseTime { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public RestResponse()
        {

        }

        public RestResponse(T _data)
        {
            Data = _data;
        }

        /// <summary>
        /// Copy's request data into a new object to allow reading of request body data
        /// </summary>
        /// <param name="request"></param>
        public RestResponse(HttpRequestMessage request)
        {
            Request = request;
            BodyString = request?.Content?.ReadAsStringAsync()?.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static async Task<RestResponse<T>> CreateAsync(HttpRequestMessage request)
        {
            var response = new RestResponse<T>
            {
                Request = request
            };

            if (request?.Content != null)
            {
                response.BodyString = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            return response;
        }

        /// <summary>
        /// Returns the data object's ToString method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Data?.ToString() ?? string.Empty;
        }
    }
}
