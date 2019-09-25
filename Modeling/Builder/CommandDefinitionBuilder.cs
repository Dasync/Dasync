namespace Dasync.Modeling
{
    public class CommandDefinitionBuilder
    {
        public CommandDefinitionBuilder(IMutableCommandDefinition commandDefinition)
        {
            CommandDefinition = commandDefinition;
        }

        public IMutableCommandDefinition CommandDefinition { get; private set; }
    }
}
