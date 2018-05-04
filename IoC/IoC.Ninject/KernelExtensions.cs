using System;
using System.Collections.Generic;
using Ninject;

namespace Dasync.Ioc.Ninject
{
    public static class KernelExtensions
    {
        public static IKernel Load(this IKernel kernel, IEnumerable<KeyValuePair<Type, Type>> bindings)
        {
            foreach (var binding in bindings)
            {
                var interfaceType = binding.Key;
                var implementationType = binding.Value;
                if (interfaceType == implementationType)
                    kernel.Rebind(implementationType).ToSelf().InSingletonScope();
                else
                    kernel.Rebind(interfaceType, implementationType).To(binding.Value).InSingletonScope();
            }
            return kernel;
        }

        public static IIocContainer ToIocContainer(this IKernel kernel)
            => new NinjectIocContainerAdapter(kernel);
    }
}
