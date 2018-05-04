using System;
using System.Threading;

namespace Dasync.EETypes.Cancellation
{
    public interface ICancellationTokenSourceRegistry
    {
        CancellationTokenSourceState Register(CancellationTokenSource source);

        bool TryGet(Guid id, out CancellationTokenSource source);
    }
}
