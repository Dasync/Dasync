using System;
using System.Collections.Generic;
using Autofac;

namespace Dasync.Ioc.Autofac
{
    public static class ContainerExtensions
    {
        public static ContainerBuilder Register(this ContainerBuilder containerBuilder, IEnumerable<KeyValuePair<Type, Type>> bindings)
        {
            foreach (var binding in bindings)
            {
                var interfaceType = binding.Key;
                var implementationType = binding.Value;
                if (interfaceType == implementationType)
                    containerBuilder.RegisterType(implementationType).AsSelf().SingleInstance();
                else
                    containerBuilder.RegisterType(implementationType).As(interfaceType).SingleInstance();
            }
            return containerBuilder;
        }

        public static IIocContainer ToIocContainer(this IContainer container)
            => new AutofaceIocContainerAdapter(container);
    }
}
