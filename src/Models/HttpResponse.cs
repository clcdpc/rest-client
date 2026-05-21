using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
        public HttpResponseHeaders Headers { get; }

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

        /// <summary>
        /// Copies data from response parameter
        /// </summary>
        /// <param name="response"></param>
        public HttpResponse(HttpResponseMessage response)
            : this(response, ReadContentSynchronously(response))
        {
        }

        public HttpResponse(HttpResponseMessage response, string content)
        {
            Content = content;
            ContentType = response.Content?.Headers?.ContentType?.ToString() ?? "";
            Headers = response.Headers;
            IsSuccessStatusCode = response.IsSuccessStatusCode;
            ReasonPhrase = response.ReasonPhrase;
            RequestMessage = response.RequestMessage;
            StatusCode = response.StatusCode;
            Version = response.Version;
        }

        private static string ReadContentSynchronously(HttpResponseMessage response)
        {
            return response.Content == null
                ? string.Empty
                : response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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
