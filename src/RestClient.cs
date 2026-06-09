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

                AddHeaders(request, httpRequest);
                AddAuthenticator(request, httpRequest);
                AddBody(request, httpRequest);
                AddParameters(request, httpRequest);

                response.Request = httpRequest;

                response.BodyString = httpRequest.Content == null
                    ? null
                    : await ReadContentAsStringAsync(httpRequest.Content, cancellationToken).ConfigureAwait(false);

                var sw = Stopwatch.StartNew();
                using var httpResponse = await SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
                response.ResponseTime = sw.ElapsedMilliseconds;
                var responseContent = httpResponse.Content == null
                    ? null
                    : await ReadContentAsStringAsync(httpResponse.Content, cancellationToken).ConfigureAwait(false);
                response.Response = new HttpResponse(httpResponse, responseContent);

                if (request.FormatOutputAsync != null)
                {
                    response.Data = (T?)await request.FormatOutputAsync(httpResponse, responseContent, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    response.Data = await FormatResponseAsync<T>(httpResponse, responseContent, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            return response;
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

            return $"{(BaseUrl?.Length > 0 ? BaseUrl.TrimEnd('/') + "/" : "")}{(!string.IsNullOrWhiteSpace(PathPrefix) ? PathPrefix.Trim('/') + "/" : "")}{path.TrimStart('/')}";
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
        /// Creates the outgoing HTTP request message for a REST request.
        /// </summary>
        /// <remarks>
        /// When no HttpClient is supplied to the constructor, RestClient uses an internally
        /// managed shared process-lifetime default transport that derived classes cannot
        /// access or dispose. Supplied HttpClient instances are used as-is and remain
        /// caller-owned. Derived classes should customize per-request behavior through
        /// RestRequest and HttpRequestMessage hooks such as this method, AddHeaders,
        /// AddAuthenticator, AddBody, and AddParameters. Transport-level behavior such as
        /// cookies, proxy, TLS, handlers, diagnostics, timeout policy, and other HttpClient
        /// or handler settings requires injecting a configured HttpClient.
        /// </remarks>
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
        /// When no HttpClient is supplied to the constructor, this method uses an internally
        /// managed shared process-lifetime default transport that derived classes cannot
        /// access or dispose. Supplied HttpClient instances are used as-is and remain
        /// caller-owned. Derived classes should use HttpRequestMessage hooks for
        /// per-request customization. Transport-level behavior such as cookies, proxy, TLS,
        /// handlers, diagnostics, timeout policy, and other HttpClient or handler settings
        /// requires injecting a configured HttpClient. Override this method only when the
        /// send operation itself is the intended extension point.
        /// </remarks>
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
