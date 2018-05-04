using System.Threading;
using System.Threading.Tasks;

namespace Dasync.EETypes.Fabric
{
    public interface IFabric
    {
        IFabricConnector GetConnector(ServiceId serviceId);

        Task InitializeAsync(CancellationToken ct);

        Task StartAsync(CancellationToken ct);

        Task TerminateAsync(CancellationToken ct);
    }
}
