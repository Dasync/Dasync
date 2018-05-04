using System;
using System.Collections.Generic;

namespace Dasync.ServiceRegistry
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IServiceRegistry)] = typeof(ServiceRegistry),
            [typeof(IServiceRegistryUpdaterViaDiscovery)] = typeof(ServiceRegistryUpdaterViaDiscovery)
        };
    }
}
