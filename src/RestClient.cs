using Clc.Rest.Auth;
using Clc.Rest.Models;
using Clc.Rest.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public IAuthenticator Authenticator { get; set; }
        public MediaTypeWithQualityHeaderValue Accept { get; set; } = new MediaTypeWithQualityHeaderValue("application/json");

        private HttpClient _client;
        protected HttpClient Client => _client ?? (_client = new HttpClient());

        protected RestClient() : this(null, null) { }
        protected RestClient(string baseUrl) : this(baseUrl, null) { }
        protected RestClient(HttpClient client) : this(null, client) { }

        protected RestClient(string baseUrl, HttpClient client)
        {
            if (!string.IsNullOrEmpty(baseUrl?.Trim()))
            {
                BaseUrl = baseUrl.Trim();
            }
            if (client != null)
            {
                _client = client;
            }
        }

        public IRestResponse<T> Get<T>(string url, Dictionary<string, string> parameters = null) =>
            ExecuteAsync<T>(new RestRequest(HttpMethod.Get, url, parameters: parameters)).Result;

        public IRestResponse<T> Post<T>(string url, Dictionary<string, string> parameters = null, object body = null) =>
            ExecuteAsync<T>(new RestRequest(HttpMethod.Post, url, body, parameters)).Result;

        public IRestResponse<T> Patch<T>(string url, Dictionary<string, string> parameters = null, object body = null) =>
            ExecuteAsync<T>(new RestRequest(new HttpMethod("PATCH"), url, body, parameters)).Result;

        public IRestResponse<T> Put<T>(string url, Dictionary<string, string> parameters = null, object body = null) =>
            ExecuteAsync<T>(new RestRequest(HttpMethod.Put, url, body, parameters)).Result;

        public IRestResponse<T> Delete<T>(string url, Dictionary<string, string> parameters = null, object body = null) =>
            ExecuteAsync<T>(new RestRequest(HttpMethod.Delete, url, body, parameters)).Result;

        public IRestResponse<T> Execute<T>(HttpMethod method, string url, Dictionary<string, string> parameters = null, object body = null) =>
            ExecuteAsync<T>(new RestRequest(method, url, body, parameters)).Result;

        public IRestResponse<T> Execute<T>(string url, HttpMethod method = null, Dictionary<string, string> parameters = null, object body = null) =>
            ExecuteAsync<T>(new RestRequest(method ?? HttpMethod.Get, url, body, parameters)).Result;

        public virtual Task<T> FormatResponseAsync<T>(HttpResponseMessage response, string content, CancellationToken cancellationToken = default)
        {
            T output = default;

            if (response.IsSuccessStatusCode)
            {
                content = PreDeserialize(content);

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

        public Task<IRestResponse<T>> ExecuteAsync<T>(string url, CancellationToken cancellationToken = default) =>
            ExecuteAsync<T>(new RestRequest(HttpMethod.Get, url), cancellationToken);

        public Task<IRestResponse<T>> ExecuteAsync<T>(HttpMethod method, string url, CancellationToken cancellationToken = default) =>
            ExecuteAsync<T>(new RestRequest(method ?? HttpMethod.Get, url), cancellationToken);

        public async Task<IRestResponse<T>> ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default)
        {
            PreformatRestRequest(request);

            var httpRequest = new HttpRequestMessage(request.Method, BuildUrl(request));
            httpRequest.Headers.Accept.Add(Accept);

            AddHeaders(request, httpRequest);
            AddAuthenticator(request, httpRequest);
            AddBody(request, httpRequest);
            AddParameters(request, httpRequest);

            var response = new RestResponse<T>(httpRequest, null);

            try
            {
                response.BodyString = httpRequest.Content == null
                    ? null
                    : await ReadContentAsStringAsync(httpRequest.Content, cancellationToken).ConfigureAwait(false);

                var sw = Stopwatch.StartNew();
                var rawResponse = await Client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
                response.ResponseTime = sw.ElapsedMilliseconds;
                var responseContent = rawResponse.Content == null
                    ? null
                    : await ReadContentAsStringAsync(rawResponse.Content, cancellationToken).ConfigureAwait(false);
                response.Response = new HttpResponse(rawResponse, responseContent);

                if (request.FormatOutputAsync != null)
                {
                    response.Data = (T)await request.FormatOutputAsync(rawResponse, responseContent, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    response.Data = await FormatResponseAsync<T>(rawResponse, responseContent, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            return response;
        }

        public IRestResponse<T> Execute<T>(RestRequest request) => ExecuteAsync<T>(request).Result;

        public virtual RestRequest PreformatRestRequest(RestRequest request) => request;
        public virtual string PreDeserialize(string responseBody) => responseBody;

        public virtual string BuildUrl(RestRequest request)
        {
            return $"{(BaseUrl?.Length > 0 ? BaseUrl.TrimEnd('/') + "/" : "")}{(!string.IsNullOrWhiteSpace(PathPrefix) ? PathPrefix.Trim('/') + "/" : "")}{request.Path.TrimStart('/')}";
        }

        protected virtual HttpRequestMessage AddBody(RestRequest request, HttpRequestMessage httpRequest)
        {
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
                httpRequest = authenticator.Authenticate(Client, httpRequest);
            }

            return httpRequest;
        }

        protected virtual HttpRequestMessage AddParameters(RestRequest request, HttpRequestMessage httpRequest)
        {
            if (!request.Parameters.Any())
            {
                return httpRequest;
            }

            if (request.Method == HttpMethod.Post && request.Body == null)
            {
                httpRequest.Content = new FormUrlEncodedContent(request.Parameters);
                return httpRequest;
            }

            if (request.Method != HttpMethod.Post)
            {
                var nonEmptyParameters = request.Parameters
                    .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Key) && !string.IsNullOrWhiteSpace(parameter.Value))
                    .Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}")
                    .ToList();

                if (nonEmptyParameters.Any())
                {
                    httpRequest.RequestUri = AppendQueryString(httpRequest.RequestUri, string.Join("&", nonEmptyParameters));
                }
            }

            return httpRequest;
        }

        private Uri AppendQueryString(Uri requestUri, string queryToAppend)
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

            var requestUriText = requestUri.OriginalString;
            var fragmentIndex = requestUriText.IndexOf('#');
            var fragment = fragmentIndex >= 0 ? requestUriText.Substring(fragmentIndex) : string.Empty;
            var beforeFragment = fragmentIndex >= 0 ? requestUriText.Substring(0, fragmentIndex) : requestUriText;
            var separator = beforeFragment.Contains("?") ? "&" : "?";
            return new Uri($"{beforeFragment}{separator}{queryToAppend}{fragment}", UriKind.Relative);
        }

        private static async Task<string> ReadContentAsStringAsync(HttpContent content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var responseText = await content.ReadAsStringAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return responseText;
        }
    }
}
