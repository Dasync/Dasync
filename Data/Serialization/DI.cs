using System.Collections.Generic;
using Dasync.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Serialization
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<ISerializerProvider, SerializerProvider>();
            services.AddSingleton<IDefaultSerializerProvider, DefaultSerializerProvider>();
            return services;
        }
    }
}
