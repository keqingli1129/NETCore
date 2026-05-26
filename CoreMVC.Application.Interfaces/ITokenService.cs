using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreMVC.Application.Interfaces
{
    public interface ITokenService
    {
        /// <summary>
        /// Obtain a usable access token for the specified user. May attempt a refresh using a stored refresh token.
        /// </summary>
        Task<string?> GetAccessTokenAsync(ClaimsPrincipal userPrincipal);

        /// <summary>
        /// Call a downstream API using the current user's access token. The token will be refreshed if possible.
        /// Returns the HttpResponseMessage or null if no token is available.
        /// </summary>
        Task<HttpResponseMessage?> CallApiWithUserTokenAsync(ClaimsPrincipal userPrincipal, string requestUri);
    }
}
