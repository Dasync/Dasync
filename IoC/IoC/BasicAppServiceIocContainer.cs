using System;
using System.Collections.Generic;

namespace Dasync.Ioc
{
    public class BasicAppServiceIocContainer : BasicIocContainer, IAppServiceIocContainer
    {
        private Dictionary<Type, ServiceBindingInfo> _services = new Dictionary<Type, ServiceBindingInfo>();

        public void DefineService(Type serviceType, Type implementationType)
        {
            Bind(serviceType, implementationType);

            _services[serviceType] = new ServiceBindingInfo
            {
                ServiceType = serviceType,
                ImplementationType = implementationType
            };
        }

        public void DefineExternalService(Type serviceType)
        {
            Bind(serviceType, () => throw new Exception());

            _services[serviceType] = new ServiceBindingInfo
            {
                ServiceType = serviceType,
                IsExternal = true
            };
        }

        public IEnumerable<ServiceBindingInfo> DiscoverServices()
        {
            return _services.Values;
        }

        public bool TryGetImplementationType(Type serviceType, out Type implementationType)
        {
            if (Bindings.TryGetValue(serviceType, out var allBindings))
            {
                foreach (var bidning in allBindings)
                {
                    implementationType = bidning.ImplementationObject as Type;
                    if (implementationType != null)
                        return true;
                }
            }
            implementationType = null;
            return false;
        }

        public void RebindService(Type serviceType, Func<object> serviceImplementationProvider)
        {
            this.Rebind(serviceType, serviceImplementationProvider);
        }
    }
}
