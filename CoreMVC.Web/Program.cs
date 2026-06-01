using CoreMVC.Application.Interfaces;
using CoreMVC.Domain.Authorization;
using CoreMVC.Infrastructure.Authorization;
using CoreMVC.Infrastructure.Data;
using CoreMVC.Infrastructure.Services;
using CoreMVC.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Security.Claims;
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

//builder.Services.AddDistributedMemoryCache(); // or AddStackExchangeRedisCache for production
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
        // Do not persist tokens into the external authentication cookie to avoid large header/cookie sizes
        // which can cause HTTP 400 Request Too Long errors when the provider returns large tokens.
        options.SaveTokens = false;
        // Recommended scopes
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        // Default callback path is /signin-oidc; Identity external login will handle the redirect
        // Map token claims if needed here
        // Process the EntraID callback to create/link a local Identity user and sign them in
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
                    var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
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
builder.Services.AddDistributedMemoryCache(); // or Redis in production
builder.Services.AddScoped<ITokenCacheService, TokenCacheService>();
// Register ICurrentUserService
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("OrdersApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OrdersApi:BaseUrl"]!);
});
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
// Register permission-based policies
builder.Services.AddAuthorization(options =>
{
    // Dynamically register a policy per permission constant
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
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Production — real Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "CoreMVC:"; // optional key prefix
    });
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    builder.Services.AddDistributedMemoryCache();
    
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
