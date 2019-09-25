using System;

namespace Dasync.Modeling
{
    [Obsolete("Will be deleted in v2 soon")]
    public class EntityProjectionDefinitionBuilder
    {
        public EntityProjectionDefinitionBuilder(IMutableEntityProjectionDefinition entityProjectionDefinition)
        {
            EntityProjectionDefinition = entityProjectionDefinition;
        }

        public IMutableEntityProjectionDefinition EntityProjectionDefinition { get; private set; }
    }
}
