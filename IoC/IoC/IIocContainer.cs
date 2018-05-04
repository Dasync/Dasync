using System;

namespace Dasync.Ioc
{
    public interface IIocContainer
    {
        object Resolve(Type serviceType);
    }
}
