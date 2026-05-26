using CoreMVC.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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

            // Read stored tokens
            var accessToken = await _userManager.GetAuthenticationTokenAsync(user, "AzureAD", "access_token");
            var refreshToken = await _userManager.GetAuthenticationTokenAsync(user, "AzureAD", "refresh_token");

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                return accessToken;
            }

            // Attempt refresh if refresh token available
            if (string.IsNullOrWhiteSpace(refreshToken)) return null;

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
                    new("client_id", clientId),
                    new("client_secret", clientSecret),
                    new("grant_type", "refresh_token"),
                    new("refresh_token", refreshToken),
                };

                var resp = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(body));
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Token refresh failed with status {Status}", resp.StatusCode);
                    return null;
                }

                var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var at = doc.RootElement.GetProperty("access_token").GetString();
                string? rt = null;
                if (doc.RootElement.TryGetProperty("refresh_token", out var rtEl)) rt = rtEl.GetString();

                if (!string.IsNullOrWhiteSpace(at))
                {
                    // Persist new tokens
                    await _userManager.SetAuthenticationTokenAsync(user, "AzureAD", "access_token", at);
                    if (!string.IsNullOrWhiteSpace(rt))
                    {
                        await _userManager.SetAuthenticationTokenAsync(user, "AzureAD", "refresh_token", rt);
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
