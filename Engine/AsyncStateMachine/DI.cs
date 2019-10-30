using System.Collections.Generic;
using Dasync.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.AsyncStateMachine
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<IAsyncStateMachineAccessorFactory, AsyncStateMachineAccessorFactory>();
            services.AddSingleton<IAsyncStateMachineMetadataBuilder, AsyncStateMachineMetadataBuilder>();
            services.AddSingleton<IAsyncStateMachineMetadataProvider, AsyncStateMachineMetadataProvider>();
            return services;
        }
    }
}
