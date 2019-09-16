using Dasync.EETypes;
using Dasync.EETypes.Proxy;
using Dasync.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.DependencyInjection
{
    public static class ServiceCollectionDasyncExtensions
    {
        public static IServiceCollection AddDomainServicesViaDasync(this IServiceCollection services, ICommunicationModel model)
        {
            foreach (var serviceDefinition in model.Services)
            {
                if (serviceDefinition.Implementation != null)
                {
                    services.Add(new ServiceDescriptor(
                        serviceDefinition.Implementation,
                        serviceProvider =>
                            serviceProvider
                            .GetService<IServiceProxyBuilder>()
                            .Build(new ServiceId { Name = serviceDefinition.Name }),
                        ServiceLifetime.Singleton));
                }

                if (serviceDefinition.Interfaces != null)
                {
                    foreach (var interfaceType in serviceDefinition.Interfaces)
                    {
                        services.Add(new ServiceDescriptor(
                            interfaceType,
                            serviceProvider =>
                                serviceDefinition.Implementation != null
                                ? serviceProvider.GetService(serviceDefinition.Implementation)
                                : serviceProvider
                                .GetService<IServiceProxyBuilder>()
                                .Build(new ServiceId { Name = serviceDefinition.Name }),
                            ServiceLifetime.Singleton));
                    }
                }
            }

            return services;
        }

    }
}
