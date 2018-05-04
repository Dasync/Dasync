namespace Dasync.EETypes.Fabric
{
    public interface IFabricConnectorFactorySelector
    {
        IFabricConnectorFactory Select(string connectorType);
    }
}
