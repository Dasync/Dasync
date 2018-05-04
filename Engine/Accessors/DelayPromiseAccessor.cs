using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public static class DelayPromiseAccessor
    {
        public static bool IsDelayPromise(Task task)
        {
            return task.GetType().Name == "DelayPromise";
        }

        private static bool TryGetTimer(Task task, out Timer timer)
        {
            timer = null;
            if (task.GetType().Name == "DelayPromise")
            {
                timer = task.GetType()
                    .GetField("Timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(task) as Timer;
            }
            return timer != null;
        }

        public static bool TryGetTimerStartAndDelay(Task delayPromise, out DateTime startTime, out TimeSpan delay)
        {
            if (TryGetTimer(delayPromise, out var timer))
            {
                timer.GetSettings(out var start, out var initialDelay, out var interval);

                if (start.HasValue && initialDelay.HasValue)
                {
                    startTime = start.Value;
                    delay = initialDelay.Value;
                    return true;
                }
            }

            startTime = default(DateTime);
            delay = default(TimeSpan);
            return false;
        }

        public static bool TryCancelTimer(Task delayPromise)
        {
            if (TryGetTimer(delayPromise, out var timer))
            {
                timer.Dispose();
                return true;
            }
            return false;
        }
    }
}
