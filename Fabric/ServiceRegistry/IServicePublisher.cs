using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.ServiceRegistry
{
    public interface IServicePublisher
    {
        Task PublishAsync(IEnumerable<ServiceRegistrationInfo> services, CancellationToken ct);
    }
}
