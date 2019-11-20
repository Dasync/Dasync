using System;
using System.Collections.Generic;

namespace Dasync.Modeling
{
    public interface IMutableServiceDefinition : IServiceDefinition, IMutablePropertyBag
    {
        new IMutableCommunicationModel Model { get; }

        new string Name { get; set; }

        bool AddAlternateName(string name);

        new ServiceType Type { get; set; }

        bool AddInterface(Type interfaceType);

        bool RemoveInterface(Type interfaceType);

        new Type Implementation { get; set; }

        new IEnumerable<IMutableMethodDefinition> Methods { get; }

        new IEnumerable<IMutableEventDefinition> Events { get; }

        IMutableMethodDefinition GetMethod(string name);

        IMutableEventDefinition GetEvent(string name);
    }
}
