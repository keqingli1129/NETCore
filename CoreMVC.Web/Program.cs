using Microsoft.EntityFrameworkCore;
using CoreMVC.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using CoreMVC.Infrastructure.Data;
using CoreMVC.Application.Interfaces;

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

// Configure external authentication providers (Google)
builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });

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
