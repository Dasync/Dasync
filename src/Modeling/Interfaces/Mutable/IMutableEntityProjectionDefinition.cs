using System;

namespace Dasync.Modeling
{
    [Obsolete("Will be deleted in v2 soon")]
    public interface IMutableEntityProjectionDefinition : IEntityProjectionDefinition, IMutablePropertyBag
    {
        new IMutableCommunicationModel Model { get; }
    }
}
