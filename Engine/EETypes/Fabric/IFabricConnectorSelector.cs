namespace Dasync.EETypes.Fabric
{
    public interface IFabricConnectorSelector
    {
        IFabricConnector Select(ServiceId serviceId);
    }
}
