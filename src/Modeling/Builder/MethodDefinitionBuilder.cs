namespace Dasync.Modeling
{
    public class MethodDefinitionBuilder
    {
        public MethodDefinitionBuilder(IMutableMethodDefinition methodDefinition)
        {
            MethodDefinition = methodDefinition;
        }

        public IMutableMethodDefinition MethodDefinition { get; private set; }

        public MethodDefinitionBuilder Ignore()
        {
            MethodDefinition.IsIgnored = true;
            return this;
        }

        public MethodDefinitionBuilder AlternateName(params string[] alternateMethodNames)
        {
            foreach (var altName in alternateMethodNames)
                MethodDefinition.AddAlternateName(altName);
            return this;
        }
    }
}
