using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncAspNetCore
{
    public static class ServiceCollectionModuleExtensions
    {
        public static IServiceCollection AddModule(
            this IServiceCollection services,
            IEnumerable<KeyValuePair<Type, Type>> module,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            return services.AddModule(module.Select(kv => (kv.Key, kv.Value)));
        }

        public static IServiceCollection AddModule(
            this IServiceCollection services,
            IEnumerable<(Type InterfaceType, Type ImplementationType)> module,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            var servicesWithMultimpleInterfaces =
                module
                .GroupBy(tuple => tuple.ImplementationType)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            foreach (var (InterfaceType, ImplementationType) in module)
            {
                ServiceDescriptor serviceDescriptor;

                if (servicesWithMultimpleInterfaces.Contains(ImplementationType)
                    && InterfaceType != ImplementationType)
                {
                    serviceDescriptor = new ServiceDescriptor(
                        InterfaceType,
                        serviceProvider => serviceProvider.GetService(ImplementationType),
                        lifetime);
                }
                else
                {
                    serviceDescriptor = new ServiceDescriptor(
                        InterfaceType,
                        ImplementationType,
                        lifetime);
                }

                services.Add(serviceDescriptor);
            }

            return services;
        }

        public static IServiceCollection AddModules(
            this IServiceCollection services,
            IEnumerable<IEnumerable<KeyValuePair<Type, Type>>> modules,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            foreach (var module in modules)
                services.AddModule(module, lifetime);
            return services;
        }

        public static IServiceCollection AddModules(
            this IServiceCollection services,
            IEnumerable<IEnumerable<(Type InterfaceType, Type ImplementationType)>> modules,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            foreach (var module in modules)
                services.AddModule(module, lifetime);
            return services;
        }

        public static IServiceCollection AddModules(
            this IServiceCollection services,
            params IEnumerable<KeyValuePair<Type, Type>>[] modules)
            => services.AddModules((IEnumerable<IEnumerable<KeyValuePair<Type, Type>>>)modules);
    }
}
