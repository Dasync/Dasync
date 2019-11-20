using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public static class TaskCompletionSourceAccessor
    {
        public static bool IsTaskCompletionSource(object target) =>
            target.GetType().IsConstructedGenericType &&
            target.GetType().GetGenericTypeDefinition() == typeof(TaskCompletionSource<>);

        public static object Create(Type taskResultType)
        {
            var taskCompletionSourceType = typeof(TaskCompletionSource<>).MakeGenericType(taskResultType);
            return Activator.CreateInstance(taskCompletionSourceType);
        }

        public static object Create(Task task)
        {
            var taskResultType = TaskAccessor.GetTaskResultType(task.GetType());
            var taskCompletionSource = Create(taskResultType);
            SetTask(taskCompletionSource, task);
            return taskCompletionSource;
        }

        public static Task GetTask(object taskCompletionSource)
        {
            var taskField = taskCompletionSource.GetType()
                .GetField("_task", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Task)taskField.GetValue(taskCompletionSource);
        }

        public static void SetTask(object taskCompletionSource, Task task)
        {
            var taskField = taskCompletionSource.GetType()
                .GetField("_task", BindingFlags.NonPublic | BindingFlags.Instance);
            taskField.SetValue(taskCompletionSource, task);
        }
    }
}
