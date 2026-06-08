using Clc.Rest.Auth;
using Clc.Rest.Models;
using Clc.Rest.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clc.Rest
{
    public abstract class RestClient : IRestClient
    {
        public virtual string BaseUrl { get; set; } = "";
        public virtual string PathPrefix { get; set; } = "";
        public ISerializer Serializer { get; set; } = new JsonNetSerializer();
        public IDeserializer Deserializer { get; set; } = new JsonNetDeserializer();
        public IAuthenticator? Authenticator { get; set; }
        public MediaTypeWithQualityHeaderValue Accept { get; set; } = new MediaTypeWithQualityHeaderValue("application/json");

        /// <summary>
        /// Gets the client-level options that configure diagnostic request and response body capture.
        /// </summary>
        /// <remarks>
        /// These options are evaluated by ExecuteAsync and do not affect the actual request or
        /// response transport content. MaxCapturedContentLength limits only the stored diagnostic
        /// strings returned to callers.
        /// </remarks>
        public RestClientDiagnosticsOptions Diagnostics { get; } = new();

        private static readonly HttpClient SharedClient = CreateSharedClient();

        private readonly HttpClient _client;

        protected RestClient() : this(null, null) { }
        protected RestClient(string? baseUrl) : this(baseUrl, null) { }
        protected RestClient(HttpClient client) : this(null, client) { }

        /// <summary>
        /// Initializes a new RestClient instance.
        /// </summary>
        /// <remarks>
        /// A null client uses the shared process-lifetime default client. A supplied client
        /// is used as-is and remains owned by the caller. Derived classes must apply
        /// request-specific state to HttpRequestMessage rather than mutating HttpClient.
        /// </remarks>
        protected RestClient(string? baseUrl, HttpClient? client)
        {
            if (!string.IsNullOrEmpty(baseUrl?.Trim()))
            {
                BaseUrl = baseUrl.Trim();
            }

            _client = client ?? SharedClient;
        }

        private static HttpClient CreateSharedClient()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                UseCookies = false
            };

            return new HttpClient(handler, disposeHandler: true);
        }


        public virtual Task<T?> FormatResponseAsync<T>(HttpResponseMessage response, string? content, CancellationToken cancellationToken = default)
        {
            T? output = default;

            if (response.IsSuccessStatusCode)
            {
                content = PreDeserialize(content ?? string.Empty);

                if (typeof(T) == typeof(string))
                {
                    output = (T)Convert.ChangeType(content, typeof(T));
                }
                else if (typeof(T) == typeof(bool))
                {
                    output = (T)Convert.ChangeType(response.IsSuccessStatusCode, typeof(T));
                }
                else
                {
                    output = Deserializer.Deserialize<T>(content);
                }
            }

            return Task.FromResult(output);
        }

        public async Task<IRestResponse<T>> ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default)
        {
            var response = new RestResponse<T>();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                request = PreformatRestRequest(request ?? throw new ArgumentNullException(nameof(request)));
                cancellationToken.ThrowIfCancellationRequested();

                var httpRequest = CreateHttpRequestMessage(request);
                response.Request = httpRequest;

                AddHeaders(request, httpRequest);
                AddBody(request, httpRequest);
                AddParameters(request, httpRequest);
                AddAuthenticator(request, httpRequest);

                response.BodyString = await CaptureRequestBodyStringAsync(
                    request,
                    httpRequest,
                    cancellationToken).ConfigureAwait(false);

                var sw = Stopwatch.StartNew();
                using var httpResponse = await SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
                response.ResponseTime = sw.ElapsedMilliseconds;
                var responseContent = httpResponse.Content == null
                    ? null
                    : await ReadContentAsStringAsync(httpResponse.Content, cancellationToken).ConfigureAwait(false);
                var capturedResponseContent = Diagnostics.CaptureResponseContent
                    ? CaptureContentString(responseContent)
                    : null;
                response.Response = new HttpResponse(httpResponse, capturedResponseContent);

                if (request.FormatOutputAsync != null)
                {
                    response.Data = (T?)await request.FormatOutputAsync(httpResponse, responseContent, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    response.Data = await FormatResponseAsync<T>(httpResponse, responseContent, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            return response;
        }


        private async Task<string?> CaptureRequestBodyStringAsync(RestRequest request, HttpRequestMessage httpRequest, CancellationToken cancellationToken)
        {
            if (httpRequest.Content == null || !ShouldCaptureRequestBodyString(request))
            {
                return null;
            }

            var content = await ReadContentAsStringAsync(
                httpRequest.Content,
                cancellationToken).ConfigureAwait(false);

            return CaptureContentString(content);
        }

        private bool ShouldCaptureRequestBodyString(RestRequest request)
        {
            if (request.Content != null)
            {
                return Diagnostics.CaptureExplicitRequestContent;
            }

            if (request.Body != null)
            {
                return Diagnostics.CaptureSerializedRequestBody;
            }

            // Content added by an AddBody override is treated like explicit/custom content.
            return Diagnostics.CaptureExplicitRequestContent;
        }

        private string? CaptureContentString(string? content)
        {
            if (content == null)
            {
                return null;
            }

            var maxLength = Diagnostics.MaxCapturedContentLength;
            if (maxLength == null)
            {
                return content;
            }

            if (maxLength < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Diagnostics.MaxCapturedContentLength),
                    Diagnostics.MaxCapturedContentLength,
                    "MaxCapturedContentLength cannot be negative.");
            }

            if (maxLength == 0)
            {
                return string.Empty;
            }

            return content.Length <= maxLength ? content : content[..maxLength.Value];
        }

        public virtual RestRequest PreformatRestRequest(RestRequest request) => request;
        public virtual string PreDeserialize(string responseBody) => responseBody;

        public virtual string BuildUrl(RestRequest request)
        {
            var path = request?.Path ?? string.Empty;

            if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri)
                && absoluteUri.IsAbsoluteUri
                && !string.Equals(absoluteUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(BaseUrl))
            {
                builder.Append(BaseUrl.TrimEnd('/')).Append('/');
            }

            if (!string.IsNullOrWhiteSpace(PathPrefix))
            {
                builder.Append(PathPrefix.Trim('/')).Append('/');
            }

            builder.Append(path.TrimStart('/'));

            return builder.ToString();
        }

        public virtual Uri BuildRequestUri(RestRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var requestUri = new Uri(BuildUrl(request), UriKind.RelativeOrAbsolute);
            var queryString = BuildQueryString(request);

            return string.IsNullOrEmpty(queryString)
                ? requestUri
                : AppendQueryString(requestUri, queryString);
        }

        /// <summary>
        /// Creates the outgoing HTTP request message for the supplied REST request.
        /// </summary>
        /// <remarks>
        /// When no HttpClient is supplied to the constructor, RestClient uses an internally
        /// managed shared process-lifetime default transport. Supplied HttpClient instances
        /// are used as-is and remain caller-owned. Derived classes cannot access or dispose
        /// the internally managed default HttpClient; customize per-request behavior by
        /// overriding HttpRequestMessage hooks such as this method, AddHeaders, AddAuthenticator,
        /// AddBody, and AddParameters. For transport-level behavior such as cookies, proxy, TLS,
        /// handlers, diagnostics, or timeout policy, inject a configured HttpClient.
        /// </remarks>
        /// <param name="request">The REST request to convert into an HTTP request message.</param>
        /// <returns>The outgoing HTTP request message.</returns>
        protected virtual HttpRequestMessage CreateHttpRequestMessage(RestRequest request)
        {
            var httpRequest = new HttpRequestMessage(request.Method, BuildRequestUri(request));
            httpRequest.Headers.Accept.Add(Accept);
            return httpRequest;
        }

        /// <summary>
        /// Sends the outgoing HTTP request message.
        /// </summary>
        /// <remarks>
        /// When no HttpClient is supplied to the constructor, RestClient uses an internally
        /// managed shared process-lifetime default transport. Supplied HttpClient instances
        /// are used as-is and remain caller-owned. Derived classes cannot access or dispose
        /// the internally managed default HttpClient; customize per-request behavior through
        /// HttpRequestMessage hooks and override this method only when the send operation itself
        /// must be customized. For transport-level behavior such as cookies, proxy, TLS,
        /// handlers, diagnostics, or timeout policy, inject a configured HttpClient.
        /// </remarks>
        /// <param name="request">The outgoing HTTP request message.</param>
        /// <param name="cancellationToken">A token that can cancel the send operation.</param>
        /// <returns>The HTTP response message.</returns>
        protected virtual Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _client.SendAsync(request, cancellationToken);
        }

        protected virtual void AddBody(RestRequest request, HttpRequestMessage httpRequest)
        {
            if (request.Content != null)
            {
                httpRequest.Content = request.Content;
                return;
            }

            if (request.Body != null)
            {
                var serializer = request.Serializer ?? Serializer;
                httpRequest.Content = new StringContent(serializer.Serialize(request.Body), Encoding.UTF8, serializer.MediaType);
            }
        }

        protected virtual void AddHeaders(RestRequest request, HttpRequestMessage httpRequest)
        {
            foreach (var header in request.Headers)
            {
                httpRequest.Headers.Add(header.Key, header.Value);
            }
        }

        protected virtual void AddAuthenticator(RestRequest request, HttpRequestMessage httpRequest)
        {
            var authenticator = request.Authenticator ?? Authenticator;
            authenticator?.Authenticate(httpRequest);
        }

        protected virtual void AddParameters(RestRequest request, HttpRequestMessage httpRequest)
        {
            var queryString = BuildQueryString(request);
            if (string.IsNullOrEmpty(queryString))
            {
                return;
            }

            if (httpRequest.RequestUri == null)
            {
                throw new InvalidOperationException("Request URI cannot be null.");
            }

            var finalRequestUri = BuildRequestUri(request);
            if (!UriEquals(httpRequest.RequestUri, finalRequestUri))
            {
                var unparameterizedRequestUri = new Uri(BuildUrl(request), UriKind.RelativeOrAbsolute);
                httpRequest.RequestUri = UriEquals(httpRequest.RequestUri, unparameterizedRequestUri)
                    ? finalRequestUri
                    : AppendQueryString(httpRequest.RequestUri, queryString);
            }
        }

        private static string BuildQueryString(RestRequest request)
        {
            if (request.QueryParameters.Count == 0)
            {
                return string.Empty;
            }

            var nonEmptyParameters = new List<string>(request.QueryParameters.Count);
            foreach (var parameter in request.QueryParameters)
            {
                if (string.IsNullOrWhiteSpace(parameter.Key) || parameter.Value == null)
                {
                    continue;
                }

                var value = ConvertQueryParameterValue(parameter.Value);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                nonEmptyParameters.Add($"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(value)}");
            }

            return string.Join("&", nonEmptyParameters);
        }

        private static string ConvertQueryParameterValue(object value)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static bool UriEquals(Uri first, Uri second)
        {
            return first.IsAbsoluteUri || second.IsAbsoluteUri
                ? string.Equals(first.ToString(), second.ToString(), StringComparison.Ordinal)
                : string.Equals(first.OriginalString, second.OriginalString, StringComparison.Ordinal);
        }

        private static Uri AppendQueryString(Uri requestUri, string queryToAppend)
        {
            if (requestUri.IsAbsoluteUri)
            {
                var uriBuilder = new UriBuilder(requestUri);
                var existingQuery = uriBuilder.Query.TrimStart('?');
                uriBuilder.Query = string.IsNullOrEmpty(existingQuery)
                    ? queryToAppend
                    : $"{existingQuery}&{queryToAppend}";
                return uriBuilder.Uri;
            }

            var originalUri = requestUri.OriginalString;
            var fragmentIndex = originalUri.IndexOf('#');
            var pathAndQuery = fragmentIndex >= 0 ? originalUri[..fragmentIndex] : originalUri;
            var fragment = fragmentIndex >= 0 ? originalUri[fragmentIndex..] : string.Empty;

            var separator = pathAndQuery.Contains('?') ? "&" : "?";
            return new Uri($"{pathAndQuery}{separator}{queryToAppend}{fragment}", UriKind.Relative);
        }

        private static async Task<string> ReadContentAsStringAsync(HttpContent content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return value;
        }

    }
}
