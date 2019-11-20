using System;
using System.Reflection;
using System.Threading;

namespace Dasync.Accessors
{
    public static class TimerQueueTimerAccessor
    {
        private static readonly Type TimerQueueTimerType =
            typeof(Timer).GetAssembly().GetType("System.Threading.TimerQueueTimer");

        private static readonly FieldInfo _m_startTicks =
            TimerQueueTimerType.GetField("_startTicks", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            TimerQueueTimerType.GetField("m_startTicks", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo _m_dueTime =
            TimerQueueTimerType.GetField("_dueTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            TimerQueueTimerType.GetField("m_dueTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo _m_period =
            TimerQueueTimerType.GetField("_period", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            TimerQueueTimerType.GetField("m_period", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo _Close =
            TimerQueueTimerType.GetMethod("Close", new Type[0]);

        public static bool IsTimerQueueTimer(Type type) =>
            TimerQueueTimerType.IsAssignableFrom(type);

        public static bool IsTimerQueueTimer(object timerObject) =>
            TimerQueueTimerType.IsAssignableFrom(timerObject.GetType());

        public static long GetStartTicks(object timerQueueTimer)
        {
            var value = _m_startTicks.GetValue(timerQueueTimer);
            if (value is long longValue)
                return longValue;
            else
                return (int)value;
        }

        public static uint GetDueTime(object timerQueueTimer) => (uint)_m_dueTime.GetValue(timerQueueTimer);

        public static uint GetPeriod(object timerQueueTimer) => (uint)_m_period.GetValue(timerQueueTimer);

        public static void GetSettings(object timerQueueTimer, out DateTime? startTime, out TimeSpan? initialDelay, out TimeSpan? interval)
        {
            var startTicks = GetStartTicks(timerQueueTimer);
            var dueTimeMs = GetDueTime(timerQueueTimer);
            var period = GetPeriod(timerQueueTimer);

            var nowTicks = TimerQueueExtensions.TickCount;
            var now = DateTime.Now;
            var systemStartTime = now - TimeSpan.FromMilliseconds(nowTicks);

            if (startTicks != 0 && startTicks != long.MaxValue && startTicks != long.MinValue && startTicks != int.MaxValue && startTicks != int.MinValue)
            {
                startTime = systemStartTime + TimeSpan.FromMilliseconds(startTicks);
            }
            else
            {
                startTime = null;
            }

            if (dueTimeMs != uint.MaxValue)
            {
                initialDelay = TimeSpan.FromMilliseconds(dueTimeMs);
            }
            else
            {
                initialDelay = null;
            }

            if (period != uint.MaxValue)
            {
                interval = TimeSpan.FromMilliseconds(period);
            }
            else
            {
                interval = null;
            }
        }

        public static void GetCallbackAndState(object timerQueueTimer, out TimerCallback callback, out object state)
        {
            var type = timerQueueTimer.GetType();

            var fiTimerCallback =
                type.GetField("_timerCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
                type.GetField("m_timerCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var fiState =
                type.GetField("_state", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
                type.GetField("m_state", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            callback = (TimerCallback)fiTimerCallback.GetValue(timerQueueTimer);
            state = fiState.GetValue(timerQueueTimer);
        }

        public static void SetCallbackAndState(object timerQueueTimer, TimerCallback callback, object state)
        {
            var type = timerQueueTimer.GetType();

            var fiTimerCallback =
                type.GetField("_timerCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
                type.GetField("m_timerCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var fiState =
                type.GetField("_state", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
                type.GetField("m_state", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            fiTimerCallback.SetValue(timerQueueTimer, callback);
            fiState.SetValue(timerQueueTimer, state);
        }

        public static void Close(object timerQueueTimer) => _Close.Invoke(timerQueueTimer, null);
    }
}
