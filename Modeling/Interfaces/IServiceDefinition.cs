using System;
using System.Collections.Generic;
using System.Reflection;

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

        IMethodDefinition FindMethod(MethodInfo methodInfo);

        IEventDefinition FindEvent(string eventName);

        IEventDefinition FindEvent(EventInfo eventInfo);
    }
}
