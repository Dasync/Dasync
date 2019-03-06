namespace Dasync.Modeling
{
    public interface IMutableEntityProjectionDefinition : IEntityProjectionDefinition, IMutablePropertyBag
    {
        new IMutableCommunicationModel Model { get; }
    }
}
