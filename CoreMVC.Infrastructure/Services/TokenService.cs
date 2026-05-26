using CoreMVC.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace CoreMVC.Infrastructure.Services
{
    // Minimal token service that reads tokens from AspNetUserTokens, attempts refresh, and calls APIs.
    public class TokenService : ITokenService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDataProtector _protector;
        private readonly TimeSpan _refreshBeforeExpiry;

        public TokenService(
            UserManager<IdentityUser> userManager,
            IHttpClientFactory httpClientFactory,
            ILogger<TokenService> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            // Create a data protector for token encryption at rest
            var provider = DataProtectionProvider.Create("CoreMVC.TokenProtection");
            _protector = provider.CreateProtector("CoreMVC.Infrastructure.Services.TokenService.v1");

            var refreshBeforeSeconds = configuration.GetValue<int?>("TokenService:RefreshBeforeExpirySeconds") ?? 60;
            _refreshBeforeExpiry = TimeSpan.FromSeconds(refreshBeforeSeconds);
        }

        public async Task<string?> GetAccessTokenAsync(ClaimsPrincipal userPrincipal)
        {
            if (userPrincipal?.Identity == null || !userPrincipal.Identity.IsAuthenticated)
            {
                return null;
            }

            var email = userPrincipal.FindFirst(ClaimTypes.Email)?.Value
                        ?? userPrincipal.FindFirst("preferred_username")?.Value
                        ?? userPrincipal.FindFirst("upn")?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            // Read stored tokens (encrypted) and attempt to unprotect them.
            var storedAccess = await _userManager.GetAuthenticationTokenAsync(user, "AzureAD", "access_token");
            var storedRefresh = await _userManager.GetAuthenticationTokenAsync(user, "AzureAD", "refresh_token");
            var storedExpiry = await _userManager.GetAuthenticationTokenAsync(user, "AzureAD", "access_token_expires_at");

            string? accessToken = null;
            string? refreshToken = null;
            DateTimeOffset? expiresAt = null;

            // helper func to try unprotect and migrate plaintext
            string? UnprotectMaybe(string? value)
            {
                if (string.IsNullOrWhiteSpace(value)) return null;
                try
                {
                    return _protector.Unprotect(value);
                }
                catch
                {
                    // value may be plaintext from older storage; return it and we'll re-protect it
                    return value;
                }
            }

            accessToken = UnprotectMaybe(storedAccess);
            refreshToken = UnprotectMaybe(storedRefresh);

            if (!string.IsNullOrWhiteSpace(storedExpiry))
            {
                var rawExpiry = UnprotectMaybe(storedExpiry) ?? storedExpiry;
                if (DateTimeOffset.TryParse(rawExpiry, out var parsed)) expiresAt = parsed;
            }

            // If access token exists and isn't near expiry, return it
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                if (expiresAt.HasValue)
                {
                    if (DateTimeOffset.UtcNow + _refreshBeforeExpiry < expiresAt.Value)
                    {
                        return accessToken;
                    }
                    // else attempt refresh below
                }
                else
                {
                    // No expiry info, return token (best effort)
                    return accessToken;
                }
            }

            // Attempt refresh if refresh token available
            if (string.IsNullOrWhiteSpace(refreshToken)) return accessToken; // maybe null

            var refreshed = await RefreshTokensAsync(refreshToken, user);
            return refreshed?.access_token;
        }

        public async Task<System.Net.Http.HttpResponseMessage?> CallApiWithUserTokenAsync(ClaimsPrincipal userPrincipal, string requestUri)
        {
            var token = await GetAccessTokenAsync(userPrincipal);
            if (string.IsNullOrWhiteSpace(token)) return null;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                return await client.GetAsync(requestUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling API {Uri}", requestUri);
                return null;
            }
        }

        private async Task<(string access_token, string? refresh_token)?> RefreshTokensAsync(string refreshToken, IdentityUser user)
        {
            try
            {
                var tenantId = _configuration["Authentication:AzureAd:TenantId"];
                var instance = _configuration["Authentication:AzureAd:Instance"] ?? "https://login.microsoftonline.com/";
                var clientId = _configuration["Authentication:AzureAd:ClientId"];
                var clientSecret = _configuration["Authentication:AzureAd:ClientSecret"];

                var tokenEndpoint = (instance.EndsWith("/") ? instance + tenantId + "/oauth2/v2.0/token" : instance + "/" + tenantId + "/oauth2/v2.0/token");

                var client = _httpClientFactory.CreateClient();
                var body = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string,string>("client_id", clientId),
                    new KeyValuePair<string,string>("client_secret", clientSecret),
                    new KeyValuePair<string,string>("grant_type", "refresh_token"),
                    new KeyValuePair<string,string>("refresh_token", refreshToken),
                };

                var resp = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(body));
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Token refresh failed with status {Status}", resp.StatusCode);
                    return null;
                }

                var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var at = doc.RootElement.TryGetProperty("access_token", out var atEl) ? atEl.GetString() : null;
                string? rt = null;
                if (doc.RootElement.TryGetProperty("refresh_token", out var rtEl)) rt = rtEl.GetString();

                int? expiresIn = null;
                if (doc.RootElement.TryGetProperty("expires_in", out var expEl) && expEl.TryGetInt32(out var expVal)) expiresIn = expVal;

                if (!string.IsNullOrWhiteSpace(at))
                {
                    try
                    {
                        // Protect tokens before storing
                        var protectedAccess = _protector.Protect(at);
                        await _userManager.SetAuthenticationTokenAsync(user, "AzureAD", "access_token", protectedAccess);

                        if (!string.IsNullOrWhiteSpace(rt))
                        {
                            var protectedRefresh = _protector.Protect(rt);
                            await _userManager.SetAuthenticationTokenAsync(user, "AzureAD", "refresh_token", protectedRefresh);
                        }

                        // Store expiry as ISO string protected
                        if (expiresIn.HasValue)
                        {
                            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn.Value);
                            var protectedExpiry = _protector.Protect(expiresAt.ToString("o"));
                            await _userManager.SetAuthenticationTokenAsync(user, "AzureAD", "access_token_expires_at", protectedExpiry);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to persist refreshed tokens");
                    }

                    return (at, rt);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing tokens");
                return null;
            }
        }
    }
}
