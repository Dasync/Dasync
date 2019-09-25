using System;
using System.Collections.Generic;

namespace Dasync.Modeling
{
    public interface IServiceDefinition : IPropertyBag
    {
        ICommunicationModel Model { get; }

        string Name { get; }

        string[] AlternateNames { get; }

        ServiceType Type { get; }

        Type[] Interfaces { get; }

        Type Implementation { get; }

        IEnumerable<IMethodDefinition> Methods { get; }

        IEnumerable<IEventDefinition> Events { get; }

        IMethodDefinition FindMethod(string methodName);

        IEventDefinition FindEvent(string eventName);
    }
}
