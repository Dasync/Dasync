using System;
using System.Collections.Generic;
using Dasync.AspNetCore.Communication;
using Dasync.EETypes.Ioc;
using DasyncAspNetCore;

namespace Dasync.AspNetCore
{
    internal static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IPlatformHttpClientProvider)] = typeof(PlatformHttpClientProvider),
            [typeof(IDomainServiceProvider)] = typeof(DomainServiceProvider),
            [typeof(DefaultServiceHttpConfigurator)] = typeof(DefaultServiceHttpConfigurator),
        };
    }
}
