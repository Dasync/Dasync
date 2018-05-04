using System.Collections.Generic;

namespace Dasync.ServiceRegistry
{
    public interface IServiceRegistry
    {
        IEnumerable<IServiceRegistration> AllRegistrations { get; }

        IServiceRegistration Register(ServiceRegistrationInfo info);
    }
}
