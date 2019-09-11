using System;
using System.Collections.Generic;

namespace Dasync.DependencyInjection
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IScopedServiceProvider)] = typeof(ScopedServiceProvider),
            [typeof(IServiceProviderScope)] = typeof(ServiceProviderScopeFactory),
        };
    }
}
