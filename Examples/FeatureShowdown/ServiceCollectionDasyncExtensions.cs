using Dasync.EETypes;
using Dasync.EETypes.Proxy;
using Dasync.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncFeatures
{
    public static class ServiceCollectionDasyncExtensions
    {
        public static IServiceCollection AddDasyncCore(this IServiceCollection services)
        {
            services.AddModules(
                Dasync.Modeling.DI.Bindings,
                Dasync.Serialization.DI.Bindings,
                Dasync.Serialization.Json.DI.Bindings,
                Dasync.Serializers.StandardTypes.DI.Bindings,
                Dasync.Serializers.EETypes.DI.Bindings,
                Dasync.Serializers.DomainTypes.DI.Bindings,
                Dasync.Proxy.DI.Bindings,
                Dasync.AsyncStateMachine.DI.Bindings,
                Dasync.ExecutionEngine.DI.Bindings,
                Dasync.Bootstrap.DI.Bindings);

            services.AddSingleton<Dasync.ExecutionEngine.IntrinsicFlow.IntrinsicRoutines>();

            return services;
        }

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
