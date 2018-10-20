using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;

namespace Dasync.Fabric.Sample.Base
{
    public interface IFabric
    {
        IFabricConnector GetConnector(ServiceId serviceId);

        Task InitializeAsync(CancellationToken ct);

        Task StartAsync(CancellationToken ct);

        Task TerminateAsync(CancellationToken ct);
    }
}
