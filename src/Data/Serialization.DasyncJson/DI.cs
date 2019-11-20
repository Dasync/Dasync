using System.Collections.Generic;
using Dasync.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Serialization.DasyncJson
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<IAssemblyResolver, AssemblyResolver>();
            services.AddSingleton<ITypeResolver, TypeResolver>();
            services.AddSingleton<IStandardSerializerFactory, StandardSerializerFactory>();
            services.AddSingleton<ITypeSerializerHelper, TypeSerializerHelper>();

            services.AddSingleton<ISerializerFactory, DasyncJsonSerializerFactory>();
            return services;
        }
    }
}
