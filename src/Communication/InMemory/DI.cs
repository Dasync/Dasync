using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.EETypes.Communication;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Communication.InMemory
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            // D-ASYNC
            services.AddSingleton<ICommunicationMethod, InMemoryCommunicationMethod>();
            services.AddSingleton<IEventingMethod, InMemoryEventingMethod>();
            services.AddSingleton<IMessageListeningMethod, InMemoryMessageListeningMethod>();

            // Internals
            services.AddSingleton<IMessageHandler, InMemoryMessageHandler>();
            services.AddSingleton<IMessageHub, MessageHub>();

            return services;
        }
    }
}
