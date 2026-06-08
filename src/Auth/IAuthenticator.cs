using System.Net.Http;

namespace Clc.Rest.Auth
{
    public interface IAuthenticator
    {
        /// <summary>
        /// Applies authentication state to the outgoing request.
        /// </summary>
        /// <remarks>
        /// Implementations must mutate only the supplied HttpRequestMessage. Do not mutate
        /// HttpClient state such as DefaultRequestHeaders, BaseAddress, Timeout, or handler behavior.
        /// </remarks>
        HttpRequestMessage Authenticate(HttpRequestMessage request);
    }
}
