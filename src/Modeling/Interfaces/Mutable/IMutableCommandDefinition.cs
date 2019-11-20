namespace Dasync.Modeling
{
    public interface IMutableCommandDefinition : ICommandDefinition, IMutablePropertyBag
    {
        new IMutableServiceDefinition Service { get; }
    }
}
