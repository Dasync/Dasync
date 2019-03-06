using System;

namespace Dasync.Modeling
{
    public interface IEntityProjectionDefinition : IPropertyBag
    {
        ICommunicationModel Model { get; }

        Type InterfaceType { get; }
    }
}
