using System;
using Dasync.EETypes;
using Dasync.EETypes.Ioc;
using Dasync.EETypes.Proxy;
using Dasync.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncAspNetCore
{
    public class DomainServiceProvider : IDomainServiceProvider
    {
#warning temporarily
        private readonly IServiceProvider _serviceProvider;

        public DomainServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }
    }

    public class DefaultDomainServiceProvider : IDomainServiceProvider
    {
        private readonly ICommunicationModelProvider _communicationModelProvider;

        public DefaultDomainServiceProvider(ICommunicationModelProvider communicationModelProvider)
        {
            _communicationModelProvider = communicationModelProvider;

            var serviceCollection = new ServiceCollection();
            AddDomainServicesViaDasync(serviceCollection, _communicationModelProvider.Model);

            var serviceProvider = new DefaultServiceProviderFactory().CreateServiceProvider(serviceCollection);
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public static IServiceCollection AddDomainServicesViaDasync(IServiceCollection services, ICommunicationModel model)
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
                            .Build(new ServiceId { ServiceName = serviceDefinition.Name }),
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
                                .Build(new ServiceId { ServiceName = serviceDefinition.Name }),
                            ServiceLifetime.Singleton));
                    }
                }
            }

            return services;
        }
    }
}
