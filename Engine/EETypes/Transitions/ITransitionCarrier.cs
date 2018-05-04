using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Intents;

namespace Dasync.EETypes.Transitions
{
    public interface ITransitionCarrier
    {
        Task SaveStateAsync(SaveStateIntent intent, CancellationToken ct);
    }
}
