using Dasync.EETypes;

namespace Dasync.Fabric.Sample.Base
{
    public interface IFabricConnectorSelector
    {
        IFabricConnector Select(ServiceId serviceId);
    }
}
