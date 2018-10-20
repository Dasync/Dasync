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

        private static bool TryGetTimer(Task task, out object timer)
        {
            timer = null;
            if (task.GetType().Name == "DelayPromise")
            {
                timer = task.GetType()
                    .GetField("Timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(task);
            }
            return timer != null;
        }

        public static bool TryGetTimerStartAndDelay(Task delayPromise, out DateTime startTime, out TimeSpan delay)
        {
            if (TryGetTimer(delayPromise, out var timerObject))
            {
                object timerQueueTimer;

                if (TimerQueueTimerAccessor.IsTimerQueueTimer(timerObject))
                {
                    timerQueueTimer = timerObject;
                }
                else if (timerObject is Timer timer)
                {
                    timerQueueTimer = timer.GetTimerQueueTimer();
                }
                else
                {
                    throw new InvalidOperationException("unknown timer type");
                }

                TimerQueueTimerAccessor.GetSettings(timerQueueTimer, out var start, out var initialDelay, out var interval);
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
            if (TryGetTimer(delayPromise, out var timerObject))
            {
                if (TimerQueueTimerAccessor.IsTimerQueueTimer(timerObject))
                {
                    TimerQueueTimerAccessor.Close(timerObject);
                }
                else if (timerObject is Timer timer)
                {
                    timer.Dispose();
                }
                else
                {
                    throw new InvalidOperationException("unknown timer type");
                }
                return true;
            }
            return false;
        }
    }
}
