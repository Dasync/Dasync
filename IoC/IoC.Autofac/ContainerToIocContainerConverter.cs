using System;
using Autofac;

namespace Dasync.Ioc.Autofac
{
    public class ContainerToIocContainerConverter : IIocContainerConverter
    {
        public Type ContainerType => typeof(IContainer);

        public IIocContainer Convert(object container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (container is IContainer autofacContainer)
                return autofacContainer.ToIocContainer();

            throw new ArgumentException($"The container must be IContainer, but got '{container.GetType()}'.");
        }
    }
}
