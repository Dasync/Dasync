using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncAspNetCore
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
            return new ServiceRebinder<TService>(services);
        }

        public struct ServiceRebinder<TService>
        {
            private readonly IServiceCollection _services;

            public ServiceRebinder(IServiceCollection services)
            {
                _services = services;
            }

            public IServiceCollection To<TServiceInstance>(TServiceInstance singleton) where TServiceInstance : TService
            {
                var serviceDescriptor = _services.First(sd => sd.ServiceType == typeof(TService));
                if (serviceDescriptor != null)
                    _services.Remove(serviceDescriptor);

                _services.Add(new ServiceDescriptor(typeof(TService), singleton));

                return _services;
            }
        }
    }
}
