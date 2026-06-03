using CoreMVC.Application;
using CoreMVC.Domain;
using CoreMVC.Infrastructure;
using SharedKernel;
using System.Configuration;

namespace CoreWebAPI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAppDI(this IServiceCollection services, IConfiguration configuration)
        {
            //services.AddScoped<IPersonService, PersonService>();
            services.AddApplicationDI()
                .AddInfrastructureDI(configuration)
                .AddDomainDI()
                .AddSharedKernelDI();
            return services;
        }
    }
}
