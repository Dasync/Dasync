using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.Serialization;
using Dasync.Serializers.EETypes.Cancellation;
using Dasync.Serializers.EETypes.Triggers;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Serializers.EETypes
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<ITypeNameShortener, EETypesNameShortener>();
            services.AddSingleton<IAssemblyNameShortener, EEAssemblyNameShortener>();
            services.AddSingleton<EETypesSerializerSelector>();
            services.AddSingleton<IObjectDecomposerSelector>(_ => _.GetService<EETypesSerializerSelector>());
            services.AddSingleton<IObjectComposerSelector>(_ => _.GetService<EETypesSerializerSelector>());
            services.AddSingleton<ServiceProxySerializer>();
            services.AddSingleton<CancellationTokenSourceSerializer>();
            services.AddSingleton<TaskCompletionSourceSerializer>();
            return services;
        }
    }
}
