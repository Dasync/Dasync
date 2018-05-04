using System;
using System.Reflection;
using System.Threading;

namespace Dasync.Accessors
{
    public static class TimerExtensions
    {
        public static void GetSettings(this Timer timer, out DateTime? startTime, out TimeSpan? initialDelay, out TimeSpan? interval)
        {
            var timerHolder = timer.GetType()
                .GetField("m_timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timer);

            var timerQueueTimer = timerHolder.GetType()
                .GetField("m_timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timerHolder);

            var m_startTicks = (int)timerQueueTimer.GetType()
                .GetField("m_startTicks", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timerQueueTimer);

            var m_dueTimeMs = (uint)timerQueueTimer.GetType()
                .GetField("m_dueTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timerQueueTimer);

            var m_period = (uint)timerQueueTimer.GetType()
                .GetField("m_period", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timerQueueTimer);

            var nowTicks = TimerQueueExtensions.TickCount;
            var now = DateTime.Now;
            var systemStartTime = now - TimeSpan.FromMilliseconds(nowTicks);

            if (m_startTicks != 0 && m_startTicks != int.MaxValue && m_startTicks != int.MinValue)
            {
                startTime = systemStartTime + TimeSpan.FromMilliseconds(m_startTicks);
            }
            else
            {
                startTime = null;
            }

            if (m_dueTimeMs != uint.MaxValue)
            {
                initialDelay = TimeSpan.FromMilliseconds(m_dueTimeMs);
            }
            else
            {
                initialDelay = null;
            }

            if (m_period != uint.MaxValue)
            {
                interval = TimeSpan.FromMilliseconds(m_period);
            }
            else
            {
                interval = null;
            }
        }

        public static void GetCallbackAndState(this Timer timer, out TimerCallback callback, out object state)
        {
            var timerHolder = timer.GetType()
                .GetField("m_timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timer);

            var timerQueueTimer = timerHolder.GetType()
                .GetField("m_timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timerHolder);

            callback = (TimerCallback)timerQueueTimer.GetType()
                .GetField("m_timerCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timerQueueTimer);

            state = timerQueueTimer.GetType()
                .GetField("m_state", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timerQueueTimer);
        }

        public static void SetCallbackAndState(this Timer timer, TimerCallback callback, object state)
        {
            var timerHolder = timer.GetType()
                .GetField("m_timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timer);

            var timerQueueTimer = timerHolder.GetType()
                .GetField("m_timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(timerHolder);

            timerQueueTimer.GetType()
                .GetField("m_timerCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(timerQueueTimer, callback);

            timerQueueTimer.GetType()
                .GetField("m_state", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(timerQueueTimer, state);
        }
    }
}
