using System;

namespace Dasync.Modeling
{
    public interface IServiceDefinition : IPropertyBag
    {
        ICommunicationModel Model { get; }

        string Name { get; }

        ServiceType Type { get; }

        Type[] Interfaces { get; }

        Type Implementation { get; }
    }
}
