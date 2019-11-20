namespace Dasync.Modeling
{
    public class QueryDefinitionBuilder : MethodDefinitionBuilder
    {
        public QueryDefinitionBuilder(IMutableQueryDefinition queryDefinition)
            : base((IMutableMethodDefinition)queryDefinition)
        {
            QueryDefinition = queryDefinition;
        }

        public IMutableQueryDefinition QueryDefinition { get; private set; }
    }
}
