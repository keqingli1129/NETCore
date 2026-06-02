using CoreMVC.Infrastructure;
using CoreMVC.Web;

var builder = WebApplication.CreateBuilder(args);

// Register services from each layer
builder.Services.AddWebDI();
builder.Services.AddInfrastructureDI(builder.Configuration);
builder.Services.AddExternalAuthentication(builder.Configuration, out var googleConfigured, out var azureConfigured);
builder.Services.AddPermissionAuthorization();
builder.Services.AddHttpClients(builder.Configuration);
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
