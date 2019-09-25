using System;
using System.Collections.Generic;

namespace Dasync.Modeling
{
    public interface ICommunicationModel : IPropertyBag
    {
        IServiceDefinition FindServiceByName(string name);

        IServiceDefinition FindServiceByInterface(Type interfaceType);

        IServiceDefinition FindServiceByImplementation(Type implementationType);

        IReadOnlyCollection<IServiceDefinition> Services { get; }

        IEntityProjectionDefinition FindEntityProjectionByIterfaceType(Type interfaceType);

        IReadOnlyCollection<IEntityProjectionDefinition> EntityProjections { get; }
    }
}
