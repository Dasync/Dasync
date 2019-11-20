using System.Collections.Generic;
using Dasync.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Proxy
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<IProxyTypeBuilder, ProxyTypeBuilder>();
            services.AddSingleton<IMethodInvokerFactory, MethodInvokerFactory>();
            return services;
        }
    }
}
