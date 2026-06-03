using CoreMVC.Application.Interfaces;
using CoreMVC.Infrastructure.Authorization;
using CoreMVC.Infrastructure.Data;
using CoreMVC.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMVC.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services: EF Core, Identity, email, caching, and authorization handlers.
    /// </summary>
    public static IServiceCollection AddInfrastructureDI(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core with SQL Server
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Identity with Roles
        services.AddDefaultIdentity<IdentityUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // Email senders (application abstraction + Identity UI)
        services.AddTransient<Application.Interfaces.IEmailSender, SmtpEmailSender>();
        services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, SmtpEmailSender>();

        // Distributed cache (in-memory default; replaced by Redis in production via Web layer)
        services.AddDistributedMemoryCache();
        services.AddScoped<ITokenCacheService, TokenCacheService>();

        // Current user
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Permission authorization handler
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
