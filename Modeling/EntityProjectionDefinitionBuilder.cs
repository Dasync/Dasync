namespace Dasync.Modeling
{
    public class EntityProjectionDefinitionBuilder
    {
        public EntityProjectionDefinitionBuilder(IMutableEntityProjectionDefinition entityProjectionDefinition)
        {
            EntityProjectionDefinition = entityProjectionDefinition;
        }

        public IMutableEntityProjectionDefinition EntityProjectionDefinition { get; private set; }
    }
}
