using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.EETypes.Communication;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Communication.RabbitMQ
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<ICommunicationMethod, RabbitMQCommunicationMethod>();
            services.AddSingleton<IEventingMethod, RabbitMQCommunicationMethod>();
            services.AddSingleton<IMessageListeningMethod, RabbitMQMessageListiningMethod>();
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<IMessageHandler, RabbitMQMessageHandler>();
            return services;
        }
    }
}
