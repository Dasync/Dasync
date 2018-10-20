using Dasync.EETypes;

namespace Dasync.Fabric.Sample.Base
{
    public interface IFabricConnectorFactory
    {
        string ConnectorType { get; }

        IFabricConnector Create(ServiceId serviceId, object configuration);
    }
}
