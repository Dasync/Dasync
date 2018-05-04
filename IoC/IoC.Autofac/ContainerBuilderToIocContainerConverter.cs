using System;
using Autofac;

namespace Dasync.Ioc.Autofac
{
    public class ContainerBuilderToIocContainerConverter : IIocContainerConverter
    {
        public Type ContainerType => typeof(ContainerBuilder);

        public IIocContainer Convert(object container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (container is ContainerBuilder containerBuilder)
                return containerBuilder.Build().ToIocContainer();

            throw new ArgumentException($"The container must be ContainerBuilder, but got '{container.GetType()}'.");
        }
    }
}
