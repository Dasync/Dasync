using System;
using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.EETypes.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Persistence.FileSystem
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            // .NET Hosting

            // D-ASYNC
            services.AddSingleton<IPersistenceMethod, FilePersistenceMethod>();

            // Internals

            return services;
        }
    }
}
