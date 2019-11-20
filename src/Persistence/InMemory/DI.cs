using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.EETypes.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Persistence.InMemory
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            // D-ASYNC
            services.AddSingleton<IPersistenceMethod, InMemoryPersistenceMethod>();
            return services;
        }
    }
}
