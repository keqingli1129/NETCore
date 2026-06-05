using System.Security.Claims;
using System.Text.Encodings.Web;
using CoreMVC.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreWebAPI.IntegrationTests;

/// <summary>
/// Custom factory that replaces the SQL Server database with an EF Core in-memory database
/// and adds a test authentication scheme so protected endpoints can be exercised.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"IntegrationTestDb_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations to avoid dual-provider conflicts.
            // In EF Core 9+, AddDbContext separately registers IDbContextOptionsConfiguration<T>
            // that holds the options action (e.g., UseSqlServer). Both must be removed to
            // prevent the SQL Server provider from persisting alongside the InMemory provider.
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1"
                             && d.ServiceType.GenericTypeArguments[0] == typeof(ApplicationDbContext)))
                .ToList();

            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Add an in-memory database for testing (unique per factory instance for isolation)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Add a test authentication scheme
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Override the default authentication scheme
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            });
        });

        builder.UseEnvironment("Development");
    }
}

/// <summary>
/// A simple authentication handler that always authenticates requests with a test identity.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
