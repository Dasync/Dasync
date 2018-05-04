using System.Linq;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;

namespace Dasync.Ioc.Autofac
{
    public static class RegistrationBuilderExtensions
    {
        public static void LocalService<TLimit, TActivatorData, TRegistrationStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> builder)
            where TActivatorData : IConcreteActivatorData
            where TRegistrationStyle : SingleRegistrationStyle
        {
            var registration = RegistrationBuilder.CreateRegistration(builder);
            var serviceType = (registration.Services.FirstOrDefault() as TypedService)?.ServiceType;
            var implementationType = (registration.Activator as ReflectionActivator)?.LimitType;

            if (serviceType != null && implementationType != null)
            {
                var serviceBinding = new ServiceBindingInfo
                {
                    ServiceType = serviceType,
                    ImplementationType = implementationType,
                    IsExternal = false
                };

                builder.SingleInstance();
                builder.ExternallyOwned();
                builder.WithMetadata(nameof(ServiceBindingInfo), serviceBinding);
            }
        }

        public static void AsExternalService<TLimit, TActivatorData, TRegistrationStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> builder)
            where TActivatorData : IConcreteActivatorData
            where TRegistrationStyle : SingleRegistrationStyle
        {
            var registration = RegistrationBuilder.CreateRegistration(builder);
            var serviceType = (registration.Services.FirstOrDefault() as TypedService)?.ServiceType;

            if (serviceType != null)
            {
                var serviceBinding = new ServiceBindingInfo
                {
                    ServiceType = serviceType,
                    IsExternal = true
                };

                builder.SingleInstance();
                builder.ExternallyOwned();
                builder.WithMetadata(nameof(ServiceBindingInfo), serviceBinding);
            }
        }
    }
}
