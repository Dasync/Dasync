using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dasync.ServiceRegistry
{
    public class ServiceRegistry : IServiceRegistry
    {
        private readonly List<ServiceRegistration> _registrations;

        public ServiceRegistry()
        {
            _registrations = new List<ServiceRegistration>();
            AllRegistrations = _registrations.Cast<IServiceRegistration>();
        }

        public IEnumerable<IServiceRegistration> AllRegistrations { get; }

        public IServiceRegistration Register(ServiceRegistrationInfo info)
        {
            var serviceType = TryResolveType(info.QualifiedServiceTypeName);

            var serviceName = string.IsNullOrEmpty(info.Name)
                ? (serviceType != null
                    ? GetServiceName(serviceType)
                    : throw new Exception("service without name or type"))
                : info.Name;
            if (string.IsNullOrEmpty(serviceName))
                throw new InvalidOperationException(
                    $"Empty service name for '{info.QualifiedServiceTypeName}', " +
                    $"because type could not be resolved.");

            var registration = new ServiceRegistration
            {
                ServiceName = serviceName,
                ServiceType = serviceType,
                ImplementationType = TryResolveType(info.QualifiedImplementationTypeName),
                IsExternal = info.IsExternal,
                IsSingleton = info.IsSingleton,
                ConnectorType = info.ConnectorType,
                ConnectorConfiguration = info.ConnectorConfiguration
            };
            _registrations.RemoveAll(r => r.ServiceName == registration.ServiceName);
            _registrations.Add(registration);
            return registration;
        }

        private static Type TryResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            return Type.GetType(
                typeName,
#if NETFX
                TryResolveAssembly,
                typeResolver: null,
#endif
                throwOnError: false);
        }

#if NETFX
        private static Assembly TryResolveAssembly(AssemblyName name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.FullName == name.FullName);
        }
#endif

#warning Same logic is in GenerateFunctions tool
        private static string GetServiceName(Type serviceType)
        {
            if (serviceType.IsInterface() &&
                serviceType.Name.Length >= 2 &&
                serviceType.Name[0] == 'I' &&
                char.IsUpper(serviceType.Name[1]))
            {
                return serviceType.Name.Substring(1);
            }
            else
            {
                return serviceType.Name;
            }
        }
    }
}
