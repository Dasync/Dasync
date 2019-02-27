using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Modeling
{
    public interface ICommunicationModel : IPropertyBag
    {
        IServiceDefinition FindServiceByName(string name);

        IServiceDefinition FindServiceByInterface(Type interfaceType);

        IServiceDefinition FindServiceByImplementation(Type implementationType);

        IReadOnlyCollection<IServiceDefinition> Services { get; }
    }
}
