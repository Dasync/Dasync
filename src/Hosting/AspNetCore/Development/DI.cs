using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.EETypes.Communication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dasync.Hosting.AspNetCore.Development
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<IHostedService, BackgroundEventSubscriber>();
            services.AddSingleton<EventingMethod>();
            services.AddSingleton<IEventingMethod>(_ => _.GetService<EventingMethod>());
            services.AddScoped<EventingMiddleware>();
            services.AddSingleton<IMessageListeningMethod, MessageListeningMethod>();
            return services;
        }
    }
}
