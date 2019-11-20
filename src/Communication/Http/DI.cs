using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.EETypes.Communication;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Communication.Http
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<ICommunicationMethod, HttpCommunicationMethod>();
            return services;
        }
    }
}
