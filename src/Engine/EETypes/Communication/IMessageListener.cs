using System.Threading;
using System.Threading.Tasks;

namespace Dasync.EETypes.Communication
{
    public interface IMessageListener
    {
        Task StopAsync(CancellationToken ct);
    }
}
