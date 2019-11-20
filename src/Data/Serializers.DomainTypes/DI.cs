using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.Serialization;
using Dasync.Serializers.DomainTypes.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Serializers.DomainTypes
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<ITypeNameShortener, DomainTypesNameShortener>();
            services.AddSingleton<DomainTypesSerializerSelector>();
            services.AddSingleton<IObjectDecomposerSelector>(_ => _.GetService<DomainTypesSerializerSelector>());
            services.AddSingleton<IObjectComposerSelector>(_ => _.GetService<DomainTypesSerializerSelector>());
            services.AddSingleton<EntityProjectionSerializer>();
            return services;
        }
    }
}
