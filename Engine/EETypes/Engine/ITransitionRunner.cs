using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Platform;

namespace Dasync.EETypes.Engine
{
    public interface ITransitionRunner
    {
        Task RunAsync(
            ITransitionCarrier transitionCarrier,
            CancellationToken ct);
    }
}
