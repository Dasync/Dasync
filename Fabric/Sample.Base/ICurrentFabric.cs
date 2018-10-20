namespace Dasync.Fabric.Sample.Base
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
