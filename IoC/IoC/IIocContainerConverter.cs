using System;

namespace Dasync.Ioc
{
    public interface IIocContainerConverter
    {
        Type ContainerType { get; }

        IIocContainer Convert(object container);
    }
}
