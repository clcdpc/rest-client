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

        protected HttpClient client;

        public RestClient() : this(new HttpClient())
        {
        }

        public RestClient(HttpClient _client = null)
        {
            client = _client ?? new HttpClient();
        }

        public RestClient(string baseUrl, HttpClient _client = null) : this(_client)
        {
            BaseUrl = baseUrl;
        }

        public  IRestResponse<T> Get<T>(string url, Dictionary<string, string> _params = null) =>
             Execute<T>(new RestRequest(HttpMethod.Get, url, parameters: _params));

        public IRestResponse<T> Post<T>(string url, Dictionary<string, string> _params = null, object body = null) =>
             Execute<T>(new RestRequest(HttpMethod.Post, url, body, _params));

        public IRestResponse<T> Patch<T>(string url, Dictionary<string, string> _params = null, object body = null) =>
             Execute<T>(new RestRequest(new HttpMethod("PATCH"), url, body, _params));

        public IRestResponse<T> Put<T>(string url, Dictionary<string, string> _params = null, object body = null) =>
             Execute<T>(new RestRequest(HttpMethod.Put, url, body, _params));

        public IRestResponse<T> Delete<T>(string url, Dictionary<string, string> _params = null, object body = null) =>
             Execute<T>(new RestRequest(HttpMethod.Delete, url, body, _params));

        public IRestResponse<T> Execute<T>(HttpMethod method, string url, Dictionary<string, string> _params = null, object body = null) =>
            Execute<T>(new RestRequest(method, url, body, _params));

        public IRestResponse<T> Execute<T>(string url, HttpMethod method = null, Dictionary<string, string> _params = null, object body = null) =>
            Execute<T>(new RestRequest(method ?? HttpMethod.Get, url, _params));

        public virtual T FormatResponse<T>(HttpResponseMessage response)
        {
            T output = default;

            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
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
        

        public IRestResponse<T> Execute<T>(RestRequest request)
        {
            PreformatRestRequest(request);

            var httpRequest = new HttpRequestMessage(request.Method, BuildUrl(request));
            httpRequest.Headers.Accept.Add(Accept);

            AddHeaders(request, httpRequest);
            AddAuthenticator(request, httpRequest);
            AddBody(request, httpRequest);
            AddParameters(request, httpRequest);

            var response = new RestResponse<T>(httpRequest);

            try
            {
                var sw = Stopwatch.StartNew();
                var _response = client.SendAsync(httpRequest).Result;
                response.ResponseTime = sw.ElapsedMilliseconds;
                response.Response = new HttpResponse(_response);

                if (request.FormatOutput != null)
                {
                    response.Data = (T)request.FormatOutput(_response);
                }
                else
                {
                    response.Data = FormatResponse<T>(_response);
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
                httpRequest = authenticator.Authenticate(client, httpRequest);
            }

            return httpRequest;
        }

        protected virtual HttpRequestMessage AddParameters(RestRequest request, HttpRequestMessage httpRequest)
        {
            if (request.Parameters.Any())
            {
                if (request.Method == HttpMethod.Get)
                {
                    var path = httpRequest.RequestUri.AbsoluteUri.TrimEnd(new[] { '?' }) + "?";
                    foreach (var qs in request.Parameters)
                    {
                        if (!string.IsNullOrWhiteSpace(qs.Value))
                        {
                            path += $"{qs.Key}={Uri.EscapeDataString(qs.Value)}&";
                        }
                    }
                    httpRequest.RequestUri = new Uri(path.TrimEnd('&'));
                }

                if (request.Method == HttpMethod.Post)
                {
                    httpRequest.Content = new FormUrlEncodedContent(request.Parameters);
                }
            }

            return httpRequest;
        }
    }
}
