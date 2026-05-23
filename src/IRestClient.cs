using Clc.Rest.Models;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Clc.Rest
{
    public interface IRestClient
    {
        Task<IRestResponse<T>> ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default);

        Task<IRestResponse<T>> ExecuteAsync<T>(string url, CancellationToken cancellationToken = default);

        Task<IRestResponse<T>> ExecuteAsync<T>(HttpMethod method, string url, CancellationToken cancellationToken = default);

        IRestResponse<T> Execute<T>(RestRequest request);
    }
}
