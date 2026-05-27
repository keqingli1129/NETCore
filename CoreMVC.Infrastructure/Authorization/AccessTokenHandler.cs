using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoreMVC.Infrastructure.Authorization
{
    public static class AccessTokenHandler
    {
        /// <summary>
        /// Parses a raw JWT access token and returns its claims.
        /// Does NOT validate signature — use only for reading claims
        /// from tokens already validated by the OIDC middleware.
        /// </summary>
        public static IEnumerable<Claim> ParseClaims(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return Enumerable.Empty<Claim>();

            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(accessToken))
                    return Enumerable.Empty<Claim>();

                var jwt = handler.ReadJwtToken(accessToken);
                return jwt.Claims;
            }
            catch
            {
                return Enumerable.Empty<Claim>();
            }
        }

        /// <summary>
        /// Extracts roles from the token's "roles" claim (Entra ID app roles).
        /// </summary>
        public static IList<string> ExtractRoles(string accessToken)
        {
            var claims = ParseClaims(accessToken);
            return claims
                .Where(c => c.Type == "roles")
                .Select(c => c.Value)
                .ToList();
        }

        /// <summary>
        /// Extracts custom permissions from a "permissions" or "scp" claim.
        /// Entra ID uses "scp" for delegated scopes, "roles" for app roles.
        /// </summary>
        public static IList<string> ExtractPermissions(string accessToken)
        {
            var claims = ParseClaims(accessToken).ToList();

            // Entra ID delegated permissions come as space-separated "scp"
            var scp = claims
                .Where(c => c.Type == "scp")
                .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            // Custom permission claims (if you add them via optional claims in app manifest)
            var custom = claims
                .Where(c => c.Type == "permissions")
                .Select(c => c.Value);

            return scp.Concat(custom).Distinct().ToList();
        }
    }
}
