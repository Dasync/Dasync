using System;
using System.Reflection;
using System.Threading;

namespace Dasync.Accessors
{
    public static class TimerQueueExtensions
    {
        private static Type TimerQueueType =
            typeof(Timer).GetAssembly().GetType("System.Threading.TimerQueue", throwOnError: true, ignoreCase: false);

        private static PropertyInfo TickCount64PropertyInfo =
            TimerQueueType?.GetProperty("TickCount64", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        private static PropertyInfo TickCountPropertyInfo =
            TimerQueueType?.GetProperty("TickCount", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        public static long TickCount
        {
            get
            {
                if (TickCount64PropertyInfo != null)
                    return (long)TickCount64PropertyInfo.GetValue(null);

                if (TickCountPropertyInfo != null)
                    return (int)TickCountPropertyInfo.GetValue(null);

                throw new InvalidOperationException();
            }
        }
    }
}
