using System;
using Ninject;

namespace Dasync.Ioc.Ninject
{
    public class KernelToIocContainerConverter : IIocContainerConverter
    {
        public Type ContainerType => typeof(IKernel);

        public IIocContainer Convert(object container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (container is IKernel kernel)
                return kernel.ToIocContainer();

            throw new ArgumentException($"The container must be IKernel, but got '{container.GetType()}'.");
        }
    }
}
