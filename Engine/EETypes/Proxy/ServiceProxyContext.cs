using Dasync.EETypes.Descriptors;
using Dasync.Modeling;

namespace Dasync.EETypes.Proxy
{
    public class ServiceProxyContext
    {
        public IServiceDefinition Definition { get; set; }

        public ServiceDescriptor Descriptor { get; set; }
    }
}
