using System;
using System.Collections.Generic;

namespace Dasync.Ioc
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IAppServiceIocContainer)] = typeof(AppServiceIocContainerProxy),
            [typeof(AppServiceIocContainerProxy.Holder)] = typeof(AppServiceIocContainerProxy.Holder),
            [typeof(IAppIocContainerProvider)] = typeof(AppIocContainerProviderProxy),
            [typeof(AppIocContainerProviderProxy.Holder)] = typeof(AppIocContainerProviderProxy.Holder),
        };
    }
}
