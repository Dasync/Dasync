using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Intents;

namespace Dasync.Fabric.Sample.Base
{
    // Temporary interface
    public interface ITransitionStateSaver
    {
        Task SaveStateAsync(SaveStateIntent intent, CancellationToken ct);
    }
}
