using System.Threading;

namespace Dasync.Accessors
{
    public class CancellationTokenSourceStateHolder
    {
        private CancellationTokenSource _source;

        private CancellationTokenSourceStateHolder() { }

        public static CancellationTokenSourceStateHolder Get(CancellationTokenSource source)
        {
            CancellationTokenSourceStateHolder holder;
            var timer = source.GetTimer();
            if (timer != null)
            {
                timer.GetCallbackAndState(out var callback, out var state);
                holder = state as CancellationTokenSourceStateHolder;
                if (holder != null)
                    return holder;
            }

            holder = new CancellationTokenSourceStateHolder
            {
                _source = source
            };

            if (timer != null)
            {
                timer.SetCallbackAndState(_timerCallback, holder);
            }
            else
            {
#warning SuppressFlow is supported in .NET Standard 2.0, also why is it needed here?
#if NETFX
                using (ExecutionContext.SuppressFlow())
#endif
                {
                    timer = new Timer(_timerCallback, holder, -1, -1);
                }
                source.SetTimer(timer);
            }

            return holder;
        }

        public object State { get; set; }

        private static void StaticTimerCallback(object state)
        {
            var holder = (CancellationTokenSourceStateHolder)state;
            CancelletionTokenSourceExtensions.TimerCallback(holder._source);
        }

        private static readonly TimerCallback _timerCallback = StaticTimerCallback;
    }
}
