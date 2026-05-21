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
        protected HttpClient Client
        {
            get
            {
                return _client ?? (_client = new HttpClient());
            }
        }

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

        public IRestResponse<T> Get<T>(string url, Dictionary<string, string> parameters = null) => GetAsync<T>(url, parameters).Result;
        public async Task<IRestResponse<T>> GetAsync<T>(string url, Dictionary<string, string> parameters = null) => 
            await ExecuteAsync<T>(new RestRequest(HttpMethod.Get, url, parameters: parameters));

        public IRestResponse<T> Post<T>(string url, Dictionary<string, string> parameters = null, object body = null) => PostAsync<T>(url, body, parameters).Result;
        public async Task<IRestResponse<T>> PostAsync<T>(string url, object body = null, Dictionary<string, string> parameters = null) =>
             await ExecuteAsync<T>(new RestRequest(HttpMethod.Post, url, body, parameters));

        public IRestResponse<T> Patch<T>(string url, Dictionary<string, string> parameters = null, object body = null) => PatchAsync<T>(url, body, parameters).Result;
        public async Task<IRestResponse<T>> PatchAsync<T>(string url, object body = null, Dictionary<string, string> parameters = null) =>
             await ExecuteAsync<T>(new RestRequest(new HttpMethod("PATCH"), url, body, parameters));

        public IRestResponse<T> Put<T>(string url, Dictionary<string, string> parameters = null, object body = null) => PutAsync<T>(url, body, parameters).Result;
        public async Task<IRestResponse<T>> PutAsync<T>(string url, object body = null, Dictionary<string, string> parameters = null) =>
             await ExecuteAsync<T>(new RestRequest(HttpMethod.Put, url, body, parameters));

        public IRestResponse<T> Delete<T>(string url, Dictionary<string, string> parameters = null, object body = null) => DeleteAsync<T>(url, body, parameters).Result;
        public async Task<IRestResponse<T>> DeleteAsync<T>(string url, object body = null, Dictionary<string, string> parameters = null) =>
             await ExecuteAsync<T>(new RestRequest(HttpMethod.Delete, url, body, parameters));

        public IRestResponse<T> Execute<T>(HttpMethod method, string url, Dictionary<string, string> parameters = null, object body = null) => ExecuteAsync<T>(method, url, parameters, body).Result;
        public async Task<IRestResponse<T>> ExecuteAsync<T>(HttpMethod method, string url, Dictionary<string, string> parameters = null, object body = null) =>
            await ExecuteAsync<T>(new RestRequest(method, url, body, parameters));

        public IRestResponse<T> Execute<T>(string url, HttpMethod method = null, Dictionary<string, string> parameters = null, object body = null) => ExecuteAsync<T>(url, method, parameters, body).Result;
        public async Task<IRestResponse<T>> ExecuteAsync<T>(string url, HttpMethod method = null, Dictionary<string, string> parameters = null, object body = null) =>
            await ExecuteAsync<T>(new RestRequest(method ?? HttpMethod.Get, url, body, parameters));

        public virtual T FormatResponse<T>(HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return FormatResponse(response, content);
        }

        protected virtual T FormatResponse<T>(HttpResponseMessage response, string content)
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

            return output;
        }

        public async Task<IRestResponse<T>> ExecuteAsync<T>(RestRequest request)
        {
            PreformatRestRequest(request);

            var httpRequest = new HttpRequestMessage(request.Method, BuildUrl(request));
            httpRequest.Headers.Accept.Add(Accept);

            AddHeaders(request, httpRequest);
            AddAuthenticator(request, httpRequest);
            AddBody(request, httpRequest);
            AddParameters(request, httpRequest);

            var response = await RestResponse<T>.CreateAsync(httpRequest).ConfigureAwait(false);

            try
            {
                var sw = Stopwatch.StartNew();
                var _response = await Client.SendAsync(httpRequest).ConfigureAwait(false);
                var responseContent = _response.Content == null
                    ? string.Empty
                    : await _response.Content.ReadAsStringAsync().ConfigureAwait(false);
                response.ResponseTime = sw.ElapsedMilliseconds;
                response.Response = new HttpResponse(_response, responseContent);

                if (request.FormatOutput != null)
                {
                    response.Data = (T)request.FormatOutput(_response);
                }
                else
                {
                    response.Data = FormatResponse(_response, responseContent);
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            return response;
        }

        public IRestResponse<T> Execute<T>(RestRequest request) => ExecuteAsync<T>(request).ConfigureAwait(false).GetAwaiter().GetResult();

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

            var originalUri = requestUri.OriginalString;
            var fragmentIndex = originalUri.IndexOf('#');
            var pathAndQuery = fragmentIndex >= 0 ? originalUri.Substring(0, fragmentIndex) : originalUri;
            var fragment = fragmentIndex >= 0 ? originalUri.Substring(fragmentIndex) : string.Empty;

            var separator = pathAndQuery.Contains("?") ? "&" : "?";
            return new Uri($"{pathAndQuery}{separator}{queryToAppend}{fragment}", UriKind.Relative);
        }

    }
}
