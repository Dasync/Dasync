using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public static class AsyncDebugging
    {
        private static FieldInfo s_asyncDebuggingEnabled;
        private static FieldInfo s_activeTasksLock;
        private static FieldInfo s_currentActiveTasks;

        static AsyncDebugging()
        {
            s_asyncDebuggingEnabled = typeof(Task).GetField("s_asyncDebuggingEnabled", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            s_activeTasksLock = typeof(Task).GetField("s_activeTasksLock", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            s_currentActiveTasks = typeof(Task).GetField("s_currentActiveTasks", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }

        public static bool IsEnabled => (bool)s_asyncDebuggingEnabled.GetValue(null);

        public static bool TryGetActiveTask(int taskId, out Task task)
        {
            if (!IsEnabled)
            {
                task = null;
                return false;
            }

            lock (ActiveTasksLock)
            {
                return CurrentActiveTasks.TryGetValue(taskId, out task);
            }
        }

        public static object ActiveTasksLock =>
            s_activeTasksLock != null
            ? s_activeTasksLock.GetValue(null)
            : CurrentActiveTasks;

        public static Dictionary<int, Task> CurrentActiveTasks =>
            (Dictionary<int, Task>)s_currentActiveTasks.GetValue(null);
    }
}
