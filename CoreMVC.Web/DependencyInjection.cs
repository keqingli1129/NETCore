using CoreMVC.Application.Interfaces;
using CoreMVC.Domain.Authorization;
using CoreMVC.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

namespace CoreMVC.Web;

/// <summary>
/// Registers web-layer services: MVC, authentication providers, authorization policies, and HTTP clients.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds MVC controllers with views.
    /// </summary>
    public static IServiceCollection AddWebDI(this IServiceCollection services)
    {
        services.AddControllersWithViews();
        return services;
    }

    /// <summary>
    /// Configures external authentication providers (SAML, Google, EntraID) based on configuration.
    /// Returns flags indicating which providers were configured.
    /// </summary>
    public static IServiceCollection AddExternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        out bool googleConfigured,
        out bool azureConfigured)
    {
        // Bind SAML settings
        services.Configure<SamlOptions>(configuration.GetSection("Saml"));

        var authBuilder = services.AddAuthentication();

        // SAML
        var samlEntity = configuration["Saml:EntityId"];
        var samlIdp = configuration["Saml:IdpSsoUrl"];
        var idpPath = configuration["Saml:IdpCertificatePath"];
        if (!string.IsNullOrWhiteSpace(samlEntity) && !string.IsNullOrWhiteSpace(samlIdp) && !string.IsNullOrWhiteSpace(idpPath))
        {
            authBuilder.AddCookie("SAML", "Skyward Login", options =>
            {
                options.LoginPath = "/Identity/Account/SamlInitiate";
            });
        }

        // Google
        var googleClientId = configuration["Authentication:Google:ClientId"];
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
        googleConfigured = false;
        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authBuilder.AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = googleClientId;
                googleOptions.ClientSecret = googleClientSecret;
            });
            googleConfigured = true;
        }

        // Azure AD / EntraID
        var azureClientId = configuration["Authentication:AzureAd:ClientId"];
        var azureClientSecret = configuration["Authentication:AzureAd:ClientSecret"];
        var azureTenantId = configuration["Authentication:AzureAd:TenantId"];
        var azureInstance = configuration["Authentication:AzureAd:Instance"] ?? "https://login.microsoftonline.com/";
        azureConfigured = false;
        if (!string.IsNullOrWhiteSpace(azureClientId) && !string.IsNullOrWhiteSpace(azureClientSecret) && !string.IsNullOrWhiteSpace(azureTenantId))
        {
            authBuilder.AddOpenIdConnect("AzureAD", "EntraID", options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.ClientId = azureClientId;
                options.ClientSecret = azureClientSecret;
                options.Authority = azureInstance.EndsWith("/") ? azureInstance + azureTenantId + "/v2.0" : azureInstance + "/" + azureTenantId + "/v2.0";
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async context =>
                    {
                        try
                        {
                            var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value
                                        ?? context.Principal?.FindFirst("preferred_username")?.Value
                                        ?? context.Principal?.FindFirst("upn")?.Value;

                            if (string.IsNullOrWhiteSpace(email)) return;

                            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                            var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                                                    .CreateLogger("OpenIdConnectEvents");

                            var user = await userManager.FindByEmailAsync(email);
                            if (user == null)
                            {
                                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                                var createResult = await userManager.CreateAsync(user);
                                if (!createResult.Succeeded)
                                {
                                    logger.LogWarning("Failed to create local user: {Errors}",
                                        string.Join(';', createResult.Errors.Select(e => e.Description)));
                                    return;
                                }
                            }

                            var nameId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            if (!string.IsNullOrWhiteSpace(nameId))
                            {
                                var userLogins = await userManager.GetLoginsAsync(user);
                                if (!userLogins.Any(l => l.LoginProvider == "AzureAD" && l.ProviderKey == nameId))
                                {
                                    var addResult = await userManager.AddLoginAsync(
                                        user, new UserLoginInfo("AzureAD", nameId, "EntraID"));
                                    if (!addResult.Succeeded)
                                        logger.LogWarning("Failed to add external login for {Email}: {Errors}",
                                            email, string.Join(';', addResult.Errors.Select(e => e.Description)));
                                }
                            }

                            var accessToken = context.TokenEndpointResponse?.AccessToken;
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                var tokenCache = context.HttpContext.RequestServices
                                                     .GetRequiredService<ITokenCacheService>();
                                var expiry = int.TryParse(context.TokenEndpointResponse?.ExpiresIn, out var s) ? s : 3600;
                                await tokenCache.SetAccessTokenAsync(user.Id, accessToken, expiry);
                            }

                            await signInManager.SignInAsync(user, isPersistent: false);
                        }
                        catch (Exception ex)
                        {
                            context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                                .CreateLogger("OpenIdConnectEvents")
                                .LogError(ex, "Error processing EntraID token validation callback");
                        }
                    },
                    OnRemoteFailure = async context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OpenIdConnectEvents");
                        logger.LogWarning(context.Failure, "External authentication failed during OIDC callback.");

                        var message = context.Failure?.Message ?? "External authentication failed";
                        if (message.Length > 1000) message = message.Substring(0, 1000);

                        var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

                        await context.HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                        context.HandleResponse();
                        if (env.IsDevelopment())
                        {
                            string provider = "External";
                            if (context.Properties?.Items != null && context.Properties.Items.TryGetValue(".AuthScheme", out var prov) && !string.IsNullOrWhiteSpace(prov))
                            {
                                provider = prov!;
                            }

                            context.Response.Redirect($"/Identity/Account/ExternalAuthDiagnostics?provider={Uri.EscapeDataString(provider)}&error={Uri.EscapeDataString(context.Failure?.GetType().FullName ?? "")}&error_description={Uri.EscapeDataString(message)}");
                        }
                        else
                        {
                            var shortMsg = message.Length > 200 ? message.Substring(0, 200) : message;
                            context.Response.Redirect($"/Identity/Account/Login?error=ExternalAuthFailed&error_description={Uri.EscapeDataString(shortMsg)}");
                        }
                    }
                };
            });

            azureConfigured = true;
        }

        return services;
    }

    /// <summary>
    /// Registers permission-based authorization policies from <see cref="Permissions"/>.
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var permission in typeof(Permissions)
                .GetNestedTypes()
                .SelectMany(t => t.GetFields())
                .Select(f => f.GetValue(null)?.ToString())
                .Where(v => v != null))
            {
                options.AddPolicy(permission!, policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission!)));
            }
        });

        return services;
    }

    /// <summary>
    /// Registers named HTTP clients.
    /// </summary>
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("OrdersApi", client =>
        {
            client.BaseAddress = new Uri(configuration["OrdersApi:BaseUrl"]!);
        });

        return services;
    }
}
