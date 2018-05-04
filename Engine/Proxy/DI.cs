using System;
using System.Collections.Generic;

namespace Dasync.Proxy
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IProxyTypeBuilder)] = typeof(ProxyTypeBuilder),
            [typeof(IMethodInvokerFactory)] = typeof(MethodInvokerFactory)
        };
    }
}
