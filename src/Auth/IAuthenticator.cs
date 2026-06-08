using System.Net.Http;

namespace Clc.Rest.Auth
{
    public interface IAuthenticator
    {
        /// <summary>
        /// Applies authentication state to a request message.
        /// </summary>
        /// <param name="request">The request message to authenticate.</param>
        /// <returns>The authenticated request message.</returns>
        /// <remarks>
        /// Implementations must apply authentication per request and must not mutate HttpClient state.
        /// </remarks>
        HttpRequestMessage Authenticate(HttpRequestMessage request);
    }
}
