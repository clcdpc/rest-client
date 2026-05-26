using Clc.Rest.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Clc.Rest
{
    public interface IRestClient
    {
        Task<IRestResponse<T>> ExecuteAsync<T>(RestRequest request, CancellationToken cancellationToken = default);
    }
}
