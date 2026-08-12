using NZWalks.MVC.ApiClients;
using NZWalks.MVC.Models;

namespace NZWalks.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var apiOptions = builder.Configuration.GetSection("NZWalksApi").Get<NZWalksApiOptions>();
            if (string.IsNullOrWhiteSpace(apiOptions?.BaseUrl))
            {
                throw new InvalidOperationException("Configuration value 'NZWalksApi:BaseUrl' not found.");
            }

            builder.Services.AddSingleton(apiOptions);
            builder.Services.AddHttpClient<INZWalksApiClient, NZWalksApiClient>(client =>
                client.BaseAddress = new Uri(apiOptions.BaseUrl));
            builder.Services.AddScoped<IRegionsApi, RegionsApi>();

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
        }
    }
}
