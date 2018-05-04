using System;

namespace Dasync.ServiceRegistry
{
    public interface IServiceRegistration
    {
        string ServiceName { get; }

        Type ServiceType { get; }

        Type ImplementationType { get; }

        bool IsExternal { get; }

        bool IsSingleton { get; }

        string ConnectorType { get; }

        object ConnectorConfiguration { get; }
    }
}
