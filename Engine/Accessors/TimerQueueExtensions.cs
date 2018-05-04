using System.Reflection;
using System.Threading;

namespace Dasync.Accessors
{
    public static class TimerQueueExtensions
    {
        public static int TickCount
        {
            get
            {
                var timerQueueType = typeof(Timer).GetAssembly().GetType(
                    "System.Threading.TimerQueue", throwOnError: true, ignoreCase: false);
                var tickCountPropertyInfo = timerQueueType.GetProperty("TickCount",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                return (int)tickCountPropertyInfo.GetValue(null);
            }
        }
    }
}
