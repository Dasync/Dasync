using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dasync.Azure.Functions.AspNetCoreMiddleware
{
    internal static class WebJobsMiddlewareExtensions
    {
        public static IServiceCollection AddMiddlewareSupport(this IServiceCollection services)
        {
            services.AddSingleton<RequestDelegateHolder>();
            services.AddSingleton<IHostedService, MiddlewareSupportService>();
            services.EnableRegisteredHostedServices();
            services.AddScoped<IMiddlewareFactory, MiddlewareFactory>();
            return services;
        }
    }
}
