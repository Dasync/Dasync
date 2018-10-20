namespace Dasync.Fabric.Sample.Base
{
    public interface IFabricConnectorFactorySelector
    {
        IFabricConnectorFactory Select(string connectorType);
    }
}
