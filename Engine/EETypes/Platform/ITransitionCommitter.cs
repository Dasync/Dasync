using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Intents;

namespace Dasync.EETypes.Platform
{
    /// <summary>
    /// Must be implemented by concrete platform.
    /// </summary>
    public interface ITransitionCommitter
    {
        Task CommitAsync(
            ScheduledActions actions,
            // Carrier is NULL when call is made outside of a transition scope, e.g. from entry point of a console app.
            ITransitionCarrier transitionCarrier,
            TransitionCommitOptions options,
            CancellationToken ct);
    }
}
