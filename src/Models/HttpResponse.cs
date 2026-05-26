using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Clc.Rest.Models
{
    /// <summary>
    /// Class to hold HttpResponseMessage data
    /// </summary>
    public class HttpResponse
    {
        /// <summary>
        /// Response content
        /// </summary>
        public string Content { get; set; }
        public string ContentType { get; set; }

        /// <summary>
        /// Headers
        /// </summary>
        public Dictionary<string, string[]> Headers { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string[]> ContentHeaders { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Does status code indicate success
        /// </summary>
        public bool IsSuccessStatusCode { get; }

        /// <summary>
        /// Reason phrase
        /// </summary>
        public string ReasonPhrase { get; set; }

        /// <summary>
        /// Request that generated the response
        /// </summary>
        public HttpRequestMessage RequestMessage { get; set; }

        /// <summary>
        /// Status code
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        public Version Version { get; set; }

        public string FormattedBody { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public HttpResponse()
        {

        }

        public HttpResponse(HttpResponseMessage response, string content)
        {
            Content = content;
            ContentType = response.Content?.Headers?.ContentType?.ToString() ?? "";
            Headers = response.Headers.ToDictionary(
                header => header.Key,
                header => header.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
            ContentHeaders = response.Content?.Headers.ToDictionary(
                header => header.Key,
                header => header.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            IsSuccessStatusCode = response.IsSuccessStatusCode;
            ReasonPhrase = response.ReasonPhrase;
            RequestMessage = response.RequestMessage;
            StatusCode = response.StatusCode;
            Version = response.Version;
        }

        /// <summary>
        /// Provides some basic response data
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"StatusCode: {StatusCode}, Content-Type: {ContentType}, Content-Length: {(Content?.Length ?? 0)}";
        }
    }
}
