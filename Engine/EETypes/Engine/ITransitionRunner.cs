using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Platform;

namespace Dasync.EETypes.Engine
{
    [Obsolete]
    public interface ITransitionRunner
    {
        [Obsolete]
        Task RunAsync(
            ITransitionCarrier transitionCarrier,
            CancellationToken ct);
    }
}
