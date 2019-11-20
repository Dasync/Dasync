namespace Dasync.Modeling
{
    public class EventDefinitionBuilder
    {
        public EventDefinitionBuilder(IMutableEventDefinition eventDefinition)
        {
            EventDefinition = eventDefinition;
        }

        public IMutableEventDefinition EventDefinition { get; private set; }

        public EventDefinitionBuilder Ignore()
        {
            EventDefinition.IsIgnored = true;
            return this;
        }
    }
}
