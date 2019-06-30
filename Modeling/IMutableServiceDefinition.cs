﻿using System;

namespace Dasync.Modeling
{
    public interface IMutableServiceDefinition : IServiceDefinition, IMutablePropertyBag
    {
        new IMutableCommunicationModel Model { get; }

        new string Name { get; set; }

        bool AddAlternativeName(string name);

        new ServiceType Type { get; set; }

        bool AddInterface(Type interfaceType);

        bool RemoveInterface(Type interfaceType);

        new Type Implementation { get; set; }
    }
}
