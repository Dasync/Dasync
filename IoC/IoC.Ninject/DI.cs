using System;
using System.Collections.Generic;

namespace Dasync.Ioc.Ninject
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IIocContainerConverter)] = typeof(KernelToIocContainerConverter)
        };
    }
}
