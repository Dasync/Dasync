using Dasync.EETypes;
using Dasync.EETypes.Proxy;

namespace Dasync.ExecutionEngine.Proxy
{
    /// <summary>
    /// Created to bypass limitations of accessing non-fully initialized proxy during event subscription,
    /// because the proxy is in constructor execution and is being built by an IoC container.
    /// </summary>
    internal class ServiceProxyBuildingContext
    {
        private static AsyncLocal<ServiceProxyBuildingContext> _current = new AsyncLocal<ServiceProxyBuildingContext>();

        private ServiceProxyBuildingContext _previousBuildingContext;
        private ServiceProxyContext _proxyContext;

        public static ServiceProxyBuildingContext EnterScope(ServiceProxyContext context)
        {
            var buildingContext = new ServiceProxyBuildingContext
            {
                _previousBuildingContext = _current.Value,
                _proxyContext = context
            };

            _current.Value = buildingContext;
            return buildingContext;
        }

        public void ExitScope()
        {
            _current.Value = _previousBuildingContext;
        }

        public static ServiceProxyContext CurrentServiceProxyContext => _current.Value?._proxyContext;
    }
}
