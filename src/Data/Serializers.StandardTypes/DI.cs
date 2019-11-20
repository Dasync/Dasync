using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.Serialization;
using Dasync.Serializers.StandardTypes.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Serializers.StandardTypes
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<ITypeNameShortener, StandardTypeNameShortener>();
            services.AddSingleton<IAssemblyNameShortener, StandardAssemblyNameShortener>();
            services.AddSingleton<IObjectDecomposerSelector, StandardTypeDecomposerSelector>();
            services.AddSingleton<IObjectComposerSelector, StandardTypeComposerSelector>();
            services.AddSingleton<ExceptionSerializer>();
            return services;
        }
    }
}
