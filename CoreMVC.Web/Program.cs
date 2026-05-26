using Microsoft.EntityFrameworkCore;
using CoreMVC.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using CoreMVC.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using CoreMVC.Web;
using System.Security.Claims;
using System.Collections.Generic;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure EF Core with SQL Server for Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity with Roles
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure external authentication providers (Google + EntraID/Azure AD) if configured
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

var azureClientId = builder.Configuration["Authentication:AzureAd:ClientId"];
var azureClientSecret = builder.Configuration["Authentication:AzureAd:ClientSecret"];
var azureTenantId = builder.Configuration["Authentication:AzureAd:TenantId"];
var azureInstance = builder.Configuration["Authentication:AzureAd:Instance"] ?? "https://login.microsoftonline.com/";

// Bind SAML settings and add authentication
builder.Services.Configure<SamlOptions>(builder.Configuration.GetSection("Saml"));

var authBuilder = builder.Services.AddAuthentication();

// Register a simple external scheme for SAML so it appears on the Identity external login list.
// Challenging this scheme will redirect to the SamlInitiate page where we build the AuthRequest.
var samlEntity = builder.Configuration["Saml:EntityId"];
var samlIdp = builder.Configuration["Saml:IdpSsoUrl"];
var idpPath = builder.Configuration["Saml:IdpCertificatePath"];
var idpPem = !string.IsNullOrEmpty(idpPath) && File.Exists(idpPath)
    ? File.ReadAllText(idpPath)
    : builder.Configuration["Saml:IdpCertificate"]; // fallback to inline (dev only)
if (!string.IsNullOrWhiteSpace(samlEntity) && !string.IsNullOrWhiteSpace(samlIdp) && !string.IsNullOrWhiteSpace(idpPath))
{
    // Display name must be non-null to show up in ExternalLogins list
    authBuilder.AddCookie("SAML", "Skyward Login", options =>
    {
        options.LoginPath = "/Identity/Account/SamlInitiate"; // our initiate page
    });
}

var googleConfigured = false;
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = googleClientId;
        googleOptions.ClientSecret = googleClientSecret;
    });

    googleConfigured = true;
}

var azureConfigured = false;
//  
if (!string.IsNullOrWhiteSpace(azureClientId) && !string.IsNullOrWhiteSpace(azureClientSecret) && !string.IsNullOrWhiteSpace(azureTenantId))
{
    // Register EntraID / Azure AD as an external OpenID Connect provider.
    // This uses the external identity scheme so it works with ASP.NET Core Identity external login flow.
    // Register OpenID Connect with a display name so it shows up as an external login provider
    authBuilder.AddOpenIdConnect("AzureAD", "EntraID", options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClientId = azureClientId;
        options.ClientSecret = azureClientSecret;
        options.Authority = azureInstance.EndsWith("/") ? azureInstance + azureTenantId + "/v2.0" : azureInstance + "/" + azureTenantId + "/v2.0";
        options.ResponseType = OpenIdConnectResponseType.Code;
        // Do not persist tokens into the external authentication cookie to avoid large header/cookie sizes.
        // We will redeem the authorization code and persist tokens server-side in AuthorizationCodeReceived.
        options.SaveTokens = false;
        // Recommended scopes
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        // Request offline_access to receive a refresh token
        options.Scope.Add("offline_access");
        // Default callback path is /signin-oidc; Identity external login will handle the redirect
        // Map token claims if needed here
        // Process the EntraID callback to create/link a local Identity user and sign them in
        options.Events = new OpenIdConnectEvents
        {
            // Redeem the authorization code manually so we don't store tokens in the external cookie
            OnAuthorizationCodeReceived = async context =>
            {
                try
                {
                    var code = context.ProtocolMessage?.Code;
                    if (string.IsNullOrWhiteSpace(code)) return;

                    var httpFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                    var client = httpFactory.CreateClient();

                    // Build token endpoint from configured instance/tenant
                    var tokenEndpoint = (azureInstance.EndsWith("/") ? azureInstance + azureTenantId + "/oauth2/v2.0/token" : azureInstance + "/" + azureTenantId + "/oauth2/v2.0/token");

                    string? redirectUri = context.ProtocolMessage?.RedirectUri;
                    if (string.IsNullOrWhiteSpace(redirectUri) && context.Properties?.Items != null)
                    {
                        context.Properties.Items.TryGetValue(".redirect_uri", out redirectUri);
                    }

                    var body = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("client_id", azureClientId!),
                        new KeyValuePair<string, string>("client_secret", azureClientSecret!),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                        new KeyValuePair<string, string>("code", code),
                    };
                    if (!string.IsNullOrWhiteSpace(redirectUri)) body.Add(new KeyValuePair<string, string>("redirect_uri", redirectUri!));

                    var resp = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(body));
                    if (!resp.IsSuccessStatusCode)
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OpenIdConnectEvents");
                        logger.LogWarning("Token endpoint returned {Status} when redeeming code", resp.StatusCode);
                        return;
                    }

                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                    var at = doc.RootElement.TryGetProperty("access_token", out var atEl) ? atEl.GetString() : null;
                    var rt = doc.RootElement.TryGetProperty("refresh_token", out var rtEl) ? rtEl.GetString() : null;
                    var idt = doc.RootElement.TryGetProperty("id_token", out var idEl) ? idEl.GetString() : null;

                    // Tell the middleware we've handled code redemption so it won't attempt its own token exchange
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(at) || !string.IsNullOrWhiteSpace(idt))
                        {
                            context.HandleCodeRedemption(at, idt);
                        }
                    }
                    catch { /* no-op if not supported */ }

                    // Persist tokens server-side after creating/linking the local user
                    var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value
                                ?? context.Principal?.FindFirst("preferred_username")?.Value
                                ?? context.Principal?.FindFirst("upn")?.Value;

                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                        var user = await userManager.FindByEmailAsync(email);
                        if (user == null)
                        {
                            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                            var createResult = await userManager.CreateAsync(user);
                            if (!createResult.Succeeded)
                            {
                                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OpenIdConnectEvents");
                                logger.LogWarning("Failed to create local user for external login: {Errors}", string.Join(';', createResult.Errors.Select(e => e.Description)));
                            }
                        }

                        // Link login if not present
                        var nameId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrWhiteSpace(nameId))
                        {
                            var userLogins = await userManager.GetLoginsAsync(user!);
                            var alreadyLinked = userLogins.Any(l => l.LoginProvider == "AzureAD" && l.ProviderKey == nameId);
                            if (!alreadyLinked)
                            {
                                var info = new UserLoginInfo("AzureAD", nameId, "EntraID");
                                var addLoginResult = await userManager.AddLoginAsync(user!, info);
                                if (!addLoginResult.Succeeded)
                                {
                                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OpenIdConnectEvents");
                                    logger.LogWarning("Failed to add external login for user {Email}: {Errors}", email, string.Join(';', addLoginResult.Errors.Select(e => e.Description)));
                                }
                            }
                        }

                        try
                        {
                            // Persist only if not already present to avoid duplicates
                            var existingAccess = await userManager.GetAuthenticationTokenAsync(user!, "AzureAD", "access_token");
                            var existingRefresh = await userManager.GetAuthenticationTokenAsync(user!, "AzureAD", "refresh_token");
                            var existingId = await userManager.GetAuthenticationTokenAsync(user!, "AzureAD", "id_token");

                            if (string.IsNullOrWhiteSpace(existingAccess) && !string.IsNullOrWhiteSpace(at))
                                await userManager.SetAuthenticationTokenAsync(user!, "AzureAD", "access_token", at!);
                            if (string.IsNullOrWhiteSpace(existingRefresh) && !string.IsNullOrWhiteSpace(rt))
                                await userManager.SetAuthenticationTokenAsync(user!, "AzureAD", "refresh_token", rt!);
                            if (string.IsNullOrWhiteSpace(existingId) && !string.IsNullOrWhiteSpace(idt))
                                await userManager.SetAuthenticationTokenAsync(user!, "AzureAD", "id_token", idt!);
                        }
                        catch (Exception ex)
                        {
                            var tokenLogger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OpenIdConnectEvents");
                            tokenLogger.LogWarning(ex, "Failed to persist external tokens for user {Email}", email);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OpenIdConnectEvents");
                    logger.LogError(ex, "Error redeeming authorization code");
                }
            },
            OnTokenValidated = async context =>
            {
                try
                {
                    var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value
                                ?? context.Principal?.FindFirst("preferred_username")?.Value
                                ?? context.Principal?.FindFirst("upn")?.Value;

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        // Nothing to do if we can't determine a user identifier
                        return;
                    }

                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                    var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OpenIdConnectEvents");

                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                        var createResult = await userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            logger.LogWarning("Failed to create local user for external login: {Errors}", string.Join(';', createResult.Errors.Select(e => e.Description)));
                            return;
                        }
                    }

                    // Link the external login if not already linked
                    var nameId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrWhiteSpace(nameId))
                    {
                        var userLogins = await userManager.GetLoginsAsync(user);
                        var alreadyLinked = userLogins.Any(l => l.LoginProvider == "AzureAD" && l.ProviderKey == nameId);
                        if (!alreadyLinked)
                        {
                            var info = new UserLoginInfo("AzureAD", nameId, "EntraID");
                            var addLoginResult = await userManager.AddLoginAsync(user, info);
                            if (!addLoginResult.Succeeded)
                            {
                                logger.LogWarning("Failed to add external login for user {Email}: {Errors}", email, string.Join(';', addLoginResult.Errors.Select(e => e.Description)));
                            }
                        }
                    }

                    // If the OpenID Connect provider returned tokens (access/refresh/id), persist them
                    // to the AspNetUserTokens table so the app can call APIs later without storing tokens in the cookie.
                    try
                    {
                        // Read tokens that the middleware persisted to the authentication properties
                        var tokens = context.Properties?.GetTokens();
                        var accessToken = tokens?.FirstOrDefault(t => t.Name == "access_token")?.Value;
                        var refreshToken = tokens?.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
                        var idToken = tokens?.FirstOrDefault(t => t.Name == "id_token")?.Value;

                        // Only persist if not already stored (idempotent). AuthorizationCodeReceived may have already saved them.
                        var existingAccess = await userManager.GetAuthenticationTokenAsync(user, "AzureAD", "access_token");
                        var existingRefresh = await userManager.GetAuthenticationTokenAsync(user, "AzureAD", "refresh_token");
                        var existingId = await userManager.GetAuthenticationTokenAsync(user, "AzureAD", "id_token");

                        if (string.IsNullOrWhiteSpace(existingAccess) && !string.IsNullOrWhiteSpace(accessToken))
                        {
                            await userManager.SetAuthenticationTokenAsync(user, "AzureAD", "access_token", accessToken);
                        }
                        if (string.IsNullOrWhiteSpace(existingRefresh) && !string.IsNullOrWhiteSpace(refreshToken))
                        {
                            await userManager.SetAuthenticationTokenAsync(user, "AzureAD", "refresh_token", refreshToken);
                        }
                        if (string.IsNullOrWhiteSpace(existingId) && !string.IsNullOrWhiteSpace(idToken))
                        {
                            await userManager.SetAuthenticationTokenAsync(user, "AzureAD", "id_token", idToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Token persistence failure shouldn't block sign-in; log for diagnostics
                        var tokenLogger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OpenIdConnectEvents");
                        tokenLogger.LogWarning(ex, "Failed to persist external tokens for user {Email}", email);
                    }

                    // Sign the user in to the application
                    await signInManager.SignInAsync(user, isPersistent: false);
                }
                catch (Exception ex)
                {
                    // Let the normal pipeline handle failures, but log for diagnostics
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OpenIdConnectEvents");
                    logger.LogError(ex, "Error processing EntraID token validation callback");
                }
            },
            OnRemoteFailure = async context =>
            {
                // Log full failure server-side for diagnostics
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OpenIdConnectEvents");
                logger.LogWarning(context.Failure, "External authentication failed during OIDC callback.");

                // Prepare values for redirect. Include a base64-encoded failure message for diagnostics.
                var message = context.Failure?.Message ?? "External authentication failed";
                if (message.Length > 1000) message = message.Substring(0, 1000);
                var encoded = Uri.EscapeDataString(message);

                // In Development, redirect to a diagnostics page with full details. In Production, redirect to login with short message.
                var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

                // Ensure the external cookie is cleared so a stale/oversized cookie doesn't cause subsequent requests to fail.
                // If the request is already rejected by the server for having headers too large this won't run; in that case clear cookies from the browser.
                await context.HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                context.HandleResponse();
                if (env.IsDevelopment())
                {
                    // Include the provider name and the error description
                    string provider = "External";
                    if (context.Properties?.Items != null && context.Properties.Items.TryGetValue(".AuthScheme", out var prov) && !string.IsNullOrWhiteSpace(prov))
                    {
                        provider = prov!;
                    }

                    context.Response.Redirect($"/Identity/Account/ExternalAuthDiagnostics?provider={Uri.EscapeDataString(provider)}&error={Uri.EscapeDataString(context.Failure?.GetType().FullName ?? "")}&error_description={encoded}");
                }
                else
                {
                    // Production: keep user-facing redirect minimal
                    var shortMsg = message.Length > 200 ? message.Substring(0, 200) : message;
                    context.Response.Redirect($"/Identity/Account/Login?error=ExternalAuthFailed&error_description={Uri.EscapeDataString(shortMsg)}");
                }

                return;
            }
        };
    });

    azureConfigured = true;
}

// Register IEmailSender (SMTP). Configure Email settings in appsettings.json under the "Email" section.
// Example appsettings.json:
// "Email": {
//   "From": "noreply@yourdomain.com",
//   "Smtp": {
//     "Host": "smtp.example.com",
//     "Port": "587",
//     "Username": "username",
//     "Password": "password",
//     "EnableSsl": "true"
//   }
// }
// Register the Infrastructure implementation for both the application abstraction and Identity UI
builder.Services.AddTransient<CoreMVC.Application.Interfaces.IEmailSender, CoreMVC.Infrastructure.Services.SmtpEmailSender>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, CoreMVC.Infrastructure.Services.SmtpEmailSender>();

// Register TokenService for reading/refreshing stored tokens and calling downstream APIs
builder.Services.AddHttpClient();
builder.Services.AddScoped<CoreMVC.Application.Interfaces.ITokenService, CoreMVC.Infrastructure.Services.TokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!googleConfigured)
{
    app.Logger.LogWarning("Google authentication not configured. Skipping Google external login registration.");
}
if (!azureConfigured)
{
    app.Logger.LogWarning("AzureAD/EntraID authentication not configured. Skipping EntraID external login registration.");
}

app.UseHttpsRedirection();
app.UseRouting();

// Authentication + Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map Identity UI endpoints (if you scaffold/use the default UI)
app.MapRazorPages();

app.Run();
