namespace Dasync.EETypes.Fabric
{
    public interface ICurrentFabric
    {
        bool IsAvailable { get; }

        IFabric Instance { get; }
    }

    public interface ICurrentFabricSetter
    {
        void SetInstance(IFabric fabric);
    }
}
