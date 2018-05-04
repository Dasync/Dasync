using System.Threading;

namespace Dasync.Accessors
{
    public static class CancellationTokenSourceStateExtensions
    {
        public static object GetState(this CancellationTokenSource source)
        {
            if (source is CancellationTokenSourceWithState sourceWithState)
                return sourceWithState.State;
            else
                return CancellationTokenSourceStateHolder.Get(source).State;
        }

        public static void SetState(this CancellationTokenSource source, object state)
        {
            if (source is CancellationTokenSourceWithState sourceWithState)
                sourceWithState.State = state;
            else
                CancellationTokenSourceStateHolder.Get(source).State = state;

        }
    }
}
