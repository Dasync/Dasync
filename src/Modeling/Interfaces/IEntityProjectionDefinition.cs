using System;

namespace Dasync.Modeling
{
    [Obsolete("Will be deleted in v2 soon")]
    public interface IEntityProjectionDefinition : IPropertyBag
    {
        ICommunicationModel Model { get; }

        Type InterfaceType { get; }
    }
}
