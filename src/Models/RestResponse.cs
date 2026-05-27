using System;
using System.Net.Http;

namespace Clc.Rest.Models
{
    public class RestResponse<T> : IRestResponse<T>
    {
        /// <summary>
        /// Deserialized data object from response XML
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// The raw response from the PAPI service
        /// </summary>
        public HttpResponse? Response { get; set; } = new HttpResponse();

        /// <summary>
        /// The request that was sent to the PAPI service
        /// </summary>
        public HttpRequestMessage? Request { get; set; } = new HttpRequestMessage();
        public string? BodyString { get; set; }

        public Exception? Exception { get; set; }

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
        /// Initializes a response with the request that was sent.
        /// Request body content is not read by this constructor.
        /// </summary>
        /// <param name="request"></param>
        public RestResponse(HttpRequestMessage request)
            : this(request, null)
        {
        }

        public RestResponse(HttpRequestMessage request, string? bodyString)
        {
            Request = request;
            BodyString = bodyString;
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
