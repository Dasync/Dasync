namespace Dasync.Modeling
{
    public interface IMutableQueryDefinition : IQueryDefinition, IMutablePropertyBag
    {
        new IMutableServiceDefinition Service { get; }
    }
}
