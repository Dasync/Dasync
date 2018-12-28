using System;
using System.Threading;

namespace Dasync.EETypes.Cancellation
{
    public interface ICancellationTokenSourceRegistry
    {
        CancellationTokenSourceState Register(CancellationTokenSource source);

        bool TryGet(long id, out CancellationTokenSource source);
    }
}
