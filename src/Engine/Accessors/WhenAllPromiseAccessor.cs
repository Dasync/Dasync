using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public static class WhenAllPromiseAccessor
    {
        public static readonly Type WhenAllPromiseType =
            typeof(Task).GetNestedType("WhenAllPromise", BindingFlags.NonPublic | BindingFlags.Public);

        public static readonly Type WhenAllPromiseGenericType =
            typeof(Task).GetNestedType("WhenAllPromise`1", BindingFlags.NonPublic | BindingFlags.Public);

        public static bool TryGetTasks(object whenAllPromise, out Array tasks)
        {
            if (whenAllPromise == null)
                throw new ArgumentNullException(nameof(whenAllPromise));

            var promiseType = whenAllPromise.GetType();
            if (promiseType.IsGenericType())
                promiseType = promiseType.GetGenericTypeDefinition();

            if (ReferenceEquals(promiseType, WhenAllPromiseType))
            {
#warning needs optimization
                var tasksField = whenAllPromise.GetType().GetField("m_tasks", BindingFlags.Instance | BindingFlags.NonPublic);
                tasks = (Array)tasksField.GetValue(whenAllPromise);
                return true;
            }
            else if (ReferenceEquals(promiseType, WhenAllPromiseGenericType))
            {
                var tasksField = whenAllPromise.GetType().GetField("m_tasks", BindingFlags.Instance | BindingFlags.NonPublic);
                tasks = (Array)tasksField.GetValue(whenAllPromise);
                return true;
            }
            else
            {
                tasks = null;
                return false;
            }
        }
    }
}
