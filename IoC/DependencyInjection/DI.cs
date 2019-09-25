using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.DependencyInjection
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings =
            new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<IScopedServiceProvider, ScopedServiceProvider>();
            services.AddSingleton<IServiceProviderScope, ServiceProviderScopeFactory>();
            return services;
        }
    }
}
