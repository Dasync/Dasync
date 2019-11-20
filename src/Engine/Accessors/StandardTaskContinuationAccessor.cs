using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public static class StandardTaskContinuationAccessor
    {
        public static readonly Type StandardTaskContinuationType =
            typeof(Task).GetAssembly().GetType(
                "System.Threading.Tasks.StandardTaskContinuation",
                throwOnError: true, ignoreCase: false);

        public static Task GetTask(object continuation)
        {
            var taskField = StandardTaskContinuationType.GetField("m_task", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (Task)taskField.GetValue(continuation);
        }

        public static TaskContinuationOptions GetOptions(object continuation)
        {
            var optionsField = StandardTaskContinuationType.GetField("m_options", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (TaskContinuationOptions)optionsField.GetValue(continuation);
        }

        public static TaskScheduler GetScheduler(object continuation)
        {
            var schedulerField = StandardTaskContinuationType.GetField("m_taskScheduler", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (TaskScheduler)schedulerField.GetValue(continuation);
        }
    }
}
