using System;
using System.Threading;

namespace Dasync.Accessors
{
    public class CancellationTokenSourceWithState : CancellationTokenSource
    {
        public CancellationTokenSourceWithState() : base() { }

        public CancellationTokenSourceWithState(TimeSpan delay) : base(delay) { }

        public CancellationTokenSourceWithState(int millisecondsDelay) : base(millisecondsDelay) { }

        public object State { get; set; }
    }
}
