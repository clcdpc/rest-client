namespace Clc.Rest
{
    /// <summary>
    /// Configures diagnostic request and response body capture for a <see cref="RestClient" /> instance.
    /// </summary>
    /// <remarks>
    /// These options control only diagnostic strings returned to callers. They do not change the
    /// request content sent over the network or the response content used internally for formatting
    /// and deserialization.
    /// </remarks>
    public sealed class RestClientDiagnosticsOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether serialized <c>RestRequest.Body</c>
        /// values are captured in <c>IRestResponse&lt;T&gt;.BodyString</c>.
        /// </summary>
        public bool CaptureSerializedRequestBody { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether caller-supplied <see cref="System.Net.Http.HttpContent" />
        /// is read and captured in <c>IRestResponse&lt;T&gt;.BodyString</c>.
        /// </summary>
        public bool CaptureExplicitRequestContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether response content is captured in
        /// <c>HttpResponse.Content</c>.
        /// </summary>
        public bool CaptureResponseContent { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of characters stored in captured diagnostic content strings,
        /// or <see langword="null" /> to leave captured strings untruncated.
        /// </summary>
        /// <remarks>
        /// This limit applies only to stored diagnostic strings, not the request body sent to the server
        /// or the full response content used internally for formatting and deserialization. A value of
        /// zero stores an empty string when content exists and capture is enabled. Negative values throw
        /// when content capture is attempted.
        /// </remarks>
        public int? MaxCapturedContentLength { get; set; }
    }
}
