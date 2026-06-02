using CoreMVC.Application;
using CoreMVC.Domain;
using CoreMVC.Infrastructure;
using SharedKernel;
using System.Configuration;

namespace CoreWebAPI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAppDI(this IServiceCollection services)
        {
            //services.AddScoped<IPersonService, PersonService>();
            services.AddApplicationDI()
                .AddInfrastructureDI()
                .AddDomainDI()
                .AddSharedKernelDI();
            return services;
        }
    }
}
