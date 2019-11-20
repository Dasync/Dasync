using System;
using System.Collections.Generic;

namespace Dasync.Proxy
{
    public interface IProxyTypeBuilder
    {
        Type Build(IEnumerable<Type> interfacesTypes);
        Type Build(Type baseClass);
    }
}
