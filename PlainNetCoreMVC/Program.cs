using Microsoft.EntityFrameworkCore;
using PlainNetCoreMVC.Models;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("MVCNetContext") ?? throw new InvalidOperationException("Connection string 'MVCNetContext' not found.");

builder.Services.AddDbContext<MVCNetContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("PlainNetCoreWebAPI", client =>
{
    var baseUrl = builder.Configuration["PlainNetCoreWebAPI:BaseUrl"]
        ?? throw new InvalidOperationException("Configuration value 'PlainNetCoreWebAPI:BaseUrl' not found.");
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
