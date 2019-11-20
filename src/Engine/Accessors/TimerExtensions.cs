using System;
using System.Reflection;
using System.Threading;

namespace Dasync.Accessors
{
    public static class TimerExtensions
    {
        private static readonly FieldInfo _timer =
            typeof(Timer).GetField("_timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            typeof(Timer).GetField("m_timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly Type TimerHolderType = typeof(Timer).GetAssembly().GetType("System.Threading.TimerHolder");

        private static readonly FieldInfo _TimerHolder_timer =
            TimerHolderType.GetField("_timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            TimerHolderType.GetField("m_timer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        public static object GetTimerQueueTimer(this Timer timer) =>
            _TimerHolder_timer.GetValue(_timer.GetValue(timer));

        public static void GetSettings(this Timer timer, out DateTime? startTime, out TimeSpan? initialDelay, out TimeSpan? interval) =>
            TimerQueueTimerAccessor.GetSettings(timer.GetTimerQueueTimer(), out startTime, out initialDelay, out interval);

        public static void GetCallbackAndState(this Timer timer, out TimerCallback callback, out object state) =>
            TimerQueueTimerAccessor.GetCallbackAndState(timer.GetTimerQueueTimer(), out callback, out state);

        public static void SetCallbackAndState(this Timer timer, TimerCallback callback, object state) =>
            TimerQueueTimerAccessor.SetCallbackAndState(timer.GetTimerQueueTimer(), callback, state);
    }
}
