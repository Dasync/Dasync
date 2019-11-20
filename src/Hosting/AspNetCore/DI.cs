using System.Collections.Generic;
using Dasync.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Hosting.AspNetCore
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddScoped<DasyncMiddleware>();
            services.AddScoped<IHttpRequestHandler, HttpRequestHandler>();
            return services;
        }
    }
}
