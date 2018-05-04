using System.Threading;
using System.Threading.Tasks;

namespace Dasync.EETypes.Transitions
{
    public interface ITransitionRunner
    {
        Task RunAsync(
            ITransitionCarrier transitionCarrier,
            ITransitionData transitionData,
            CancellationToken ct);
    }
}
