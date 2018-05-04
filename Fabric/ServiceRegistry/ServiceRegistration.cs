using System;

namespace Dasync.ServiceRegistry
{
    public class ServiceRegistration : IServiceRegistration
    {
        public string ServiceName { get; set; }

        public Type ServiceType { get; set; }

        public Type ImplementationType { get; set; }

        public bool IsExternal { get; set; }

        public bool IsSingleton { get; set; }

        public string ConnectorType { get; set; }

        public object ConnectorConfiguration { get; set; }
    }
}
