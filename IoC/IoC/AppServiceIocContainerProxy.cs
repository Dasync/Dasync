using System;
using System.Collections.Generic;

namespace Dasync.Ioc
{
    public sealed class AppServiceIocContainerProxy : IAppServiceIocContainer
    {
        public sealed class Holder
        {
            public IAppServiceIocContainer Container { get; set; }
        }

        private Holder _holder;

        public AppServiceIocContainerProxy(Holder holder)
        {
            _holder = holder;
        }

        public IEnumerable<ServiceBindingInfo> DiscoverServices()
        {
            EnsureInnerContainer();
            return _holder.Container.DiscoverServices();
        }

        public bool TryGetImplementationType(Type serviceType, out Type implementationType)
        {
            EnsureInnerContainer();
            return _holder.Container.TryGetImplementationType(serviceType, out implementationType);
        }

        public void RebindService(Type serviceType, Func<object> serviceImplementationProvider)
        {
            EnsureInnerContainer();
            _holder.Container.RebindService(serviceType, serviceImplementationProvider);
        }

        public object Resolve(Type serviceType)
        {
            EnsureInnerContainer();
            return _holder.Container.Resolve(serviceType);
        }

        private void EnsureInnerContainer()
        {
            if (_holder.Container == null)
                throw new InvalidOperationException(
                    $"Must use the bootstrapper to initialize the IoC container.");
        }
    }
}
