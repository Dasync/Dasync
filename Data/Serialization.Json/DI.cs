using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.Serialization.Json.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Serialization.Json
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<ISerializerFactory, JsonSerializerAdapterFactory>();
            services.AddSingleton<TypeNameConverter>();
            return services;
        }
   }
}
