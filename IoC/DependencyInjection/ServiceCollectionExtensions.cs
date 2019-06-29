using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Remove<TService>(this IServiceCollection services)
        {
            var serviceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(TService));
            if (serviceDescriptor != null)
                services.Remove(serviceDescriptor);
            return services;
        }

        public static ServiceRebinder<TService> Rebind<TService>(this IServiceCollection services)
        {
            var serviceDescriptor = services.First(sd => sd.ServiceType == typeof(TService));
            if (serviceDescriptor != null)
                services.Remove(serviceDescriptor);
            return new ServiceRebinder<TService>(services, serviceDescriptor);
        }

        public struct ServiceRebinder<TService>
        {
            private readonly IServiceCollection _services;
            private readonly ServiceDescriptor _previousDescriptor;

            public ServiceRebinder(IServiceCollection services, ServiceDescriptor previousDescriptor)
            {
                _services = services;
                _previousDescriptor = previousDescriptor;
            }

            public IServiceCollection To<TServiceInstance>(TServiceInstance singleton)
                where TServiceInstance : TService
            {
                _services.Add(new ServiceDescriptor(typeof(TService), singleton));
                return _services;
            }

            public IServiceCollection To<TServiceImplementation>(ServiceLifetime? serviceLifetime = null)
                where TServiceImplementation : TService
            {
                _services.Add(new ServiceDescriptor(typeof(TService), typeof(TServiceImplementation),
                    serviceLifetime ?? _previousDescriptor?.Lifetime ?? ServiceLifetime.Singleton));
                return _services;
            }
        }
    }
}
