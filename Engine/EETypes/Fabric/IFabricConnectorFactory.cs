namespace Dasync.EETypes.Fabric
{
    public interface IFabricConnectorFactory
    {
        string ConnectorType { get; }

        IFabricConnector Create(ServiceId serviceId, object configuration);
    }
}
