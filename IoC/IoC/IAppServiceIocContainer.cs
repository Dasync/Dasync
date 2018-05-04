using System;
using System.Collections.Generic;

namespace Dasync.Ioc
{
    public interface IAppServiceIocContainer : IIocContainer
    {
        IEnumerable<ServiceBindingInfo> DiscoverServices();

#warning most likely this method is not needed
        bool TryGetImplementationType(Type serviceType, out Type implementationType);

        void RebindService(Type serviceType, Func<object> serviceImplementationProvider);
    }
}
