using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.EETypes.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Persistence.Cassandra
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<IPersistenceMethod, CassandraPersistenceMethod>();
            return services;
        }
    }
}
