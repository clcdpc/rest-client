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

        /// <summary>
        /// Gets the HTTP client used for request transport.
        /// </summary>
        /// <remarks>
        /// When no client is supplied to the constructor, RestClient uses a shared
        /// process-lifetime HttpClient. Derived classes must not mutate shared client
        /// state such as DefaultRequestHeaders, BaseAddress, Timeout, or handler-related
        /// behavior. Apply request-specific state to HttpRequestMessage instead.
        /// When a client is supplied, the caller owns its lifetime.
        /// </remarks>
        protected HttpClient Client => _client;

        protected RestClient() : this(null, null) { }
        protected RestClient(string? baseUrl) : this(baseUrl, null) { }
        protected RestClient(HttpClient client) : this(null, client) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestClient"/> class.
        /// </summary>
        /// <param name="baseUrl">The optional base URL used to build relative request paths.</param>
        /// <param name="client">
        /// The optional caller-owned HTTP client. When null, the shared process-lifetime default client is used.
        /// </param>
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

                var httpRequest = new HttpRequestMessage(request.Method, BuildRequestUri(request));
                httpRequest.Headers.Accept.Add(Accept);

                httpRequest = AddHeaders(request, httpRequest);
                httpRequest = AddAuthenticator(request, httpRequest);
                httpRequest = AddBody(request, httpRequest);
                httpRequest = AddParameters(request, httpRequest);

                response.Request = httpRequest;

                response.BodyString = httpRequest.Content == null
                    ? null
                    : await ReadContentAsStringAsync(httpRequest.Content, cancellationToken).ConfigureAwait(false);

                var sw = Stopwatch.StartNew();
                using var httpResponse = await Client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
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

        protected virtual HttpRequestMessage AddBody(RestRequest request, HttpRequestMessage httpRequest)
        {
            if (request.Content != null)
            {
                httpRequest.Content = request.Content;
                return httpRequest;
            }

            if (request.Body != null)
            {
                var serializer = request.Serializer ?? Serializer;
                httpRequest.Content = new StringContent(serializer.Serialize(request.Body), Encoding.UTF8, serializer.MediaType);
            }

            return httpRequest;
        }

        protected virtual HttpRequestMessage AddHeaders(RestRequest request, HttpRequestMessage httpRequest)
        {
            foreach (var header in request.Headers)
            {
                httpRequest.Headers.Add(header.Key, header.Value);
            }

            return httpRequest;
        }

        protected virtual HttpRequestMessage AddAuthenticator(RestRequest request, HttpRequestMessage httpRequest)
        {
            var authenticator = request.Authenticator ?? Authenticator;
            if (authenticator != null)
            {
                httpRequest = authenticator.Authenticate(httpRequest);
            }

            return httpRequest;
        }

        protected virtual HttpRequestMessage AddParameters(RestRequest request, HttpRequestMessage httpRequest)
        {
            var queryString = BuildQueryString(request);
            if (string.IsNullOrEmpty(queryString))
            {
                return httpRequest;
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

            return httpRequest;
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
