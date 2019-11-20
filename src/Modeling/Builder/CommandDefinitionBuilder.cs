namespace Dasync.Modeling
{
    public class CommandDefinitionBuilder : MethodDefinitionBuilder
    {
        public CommandDefinitionBuilder(IMutableCommandDefinition commandDefinition)
            : base((IMutableMethodDefinition)commandDefinition)
        {
            CommandDefinition = commandDefinition;
        }

        public IMutableCommandDefinition CommandDefinition { get; private set; }
    }
}
