using Dasync.EETypes;

namespace Dasync.Fabric.Sample.Base
{
    public class FabricConnectorSelector : IFabricConnectorSelector
    {
        private readonly ICurrentFabric _currentFabric;

        public FabricConnectorSelector(
            ICurrentFabric currentFabric)
        {
            _currentFabric = currentFabric;
        }

        public IFabricConnector Select(ServiceId serviceId)
        {
            return _currentFabric.Instance.GetConnector(serviceId);
        }
    }
}
