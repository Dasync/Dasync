using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public static class TaskAwaiterUtils
    {
        private static readonly ConcurrentDictionary<Type, bool> _isAwaiterTypeMap = new ConcurrentDictionary<Type, bool>();
        private static readonly Func<Type, bool> _isAwaiterTypeFunc = IsAwaiterTypeInternal;
        private static readonly Type[] _emptyTypeArray = new Type[0];

        public static bool IsAwaiterType(Type type)
        {
#warning ConfiguredTaskAwaiter implements INotifyCompletion, what was the idea here?
            //if (typeof(INotifyCompletion).IsAssignableFrom(type))
            //    return false;

            return _isAwaiterTypeMap.GetOrAdd(type, _isAwaiterTypeFunc);
        }

        private static bool IsAwaiterTypeInternal(Type type)
        {
            var isCompletedProp = type.GetProperty("IsCompleted");
            if (isCompletedProp == null || isCompletedProp.PropertyType != typeof(bool))
                return false;

            return type.GetMethod("GetResult", _emptyTypeArray) != null;
        }

        public static Task GetTask(object awaiter)
        {
#warning Optimize and re-factor this

            var taskField = awaiter.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(fi => typeof(Task).IsAssignableFrom(fi.FieldType))
                .SingleOrDefault();

#warning There is no task in yield awaiter
            return (Task)taskField?.GetValue(awaiter);
        }

        public static void SetTask(object awaiter, Task task)
        {
#warning Optimize and re-factor this

            var taskField = awaiter.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(fi => typeof(Task).IsAssignableFrom(fi.FieldType))
                .SingleOrDefault();

#warning There is no task in yield awaiter
            taskField?.SetValue(awaiter, task);
        }
    }
}
