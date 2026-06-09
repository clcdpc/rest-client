namespace Clc.Rest
{
    /// <summary>
    /// Configures diagnostic request and response body strings stored on REST responses.
    /// </summary>
    public sealed class RestClientDiagnosticsOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether request bodies serialized by the library
        /// from <see cref="Clc.Rest.Models.RestRequest.Body" /> are captured in BodyString.
        /// </summary>
        public bool CaptureSerializedRequestBody { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether caller-supplied explicit HttpContent
        /// from <see cref="Clc.Rest.Models.RestRequest.Content" /> is captured in BodyString.
        /// </summary>
        public bool CaptureExplicitRequestContent { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether response content is stored on HttpResponse.Content.
        /// Response content is still read internally for formatting and deserialization when this is false.
        /// </summary>
        public bool CaptureResponseContent { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of characters stored in diagnostic body strings.
        /// Null disables truncation; zero captures empty strings; negative values throw when capture is attempted.
        /// This limit does not change sent request content or the response string used for deserialization.
        /// </summary>
        public int? MaxCapturedContentLength { get; set; }
    }
}
