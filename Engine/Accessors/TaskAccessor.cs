using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public static class TaskAccessor
    {
        public static readonly Type VoidTaskResultType =
            typeof(Task).GetAssembly().GetType(
                "System.Threading.Tasks.VoidTaskResult",
                throwOnError: true, ignoreCase: false);

        public static readonly object CompletionSentinel =
            typeof(Task)
            .GetField("s_taskCompletionSentinel", BindingFlags.Static | BindingFlags.NonPublic)
            .GetValue(null);

        public static readonly Type WhenAllPromiseType =
            typeof(Task).GetNestedType("WhenAllPromise",
                BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly Type WhenAllPromiseGenericType =
            typeof(Task).GetNestedType("WhenAllPromise`1",
                BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly Task CompletedTask;

        private static readonly Func<Task, object> _getContinutationObjet;

        static TaskAccessor()
        {
            _getContinutationObjet = CompileGetContinuationObject();
            CompletedTask = CreateTask(null);
            CompletedTask.TrySetResult(null);
        }

        public static Task CreateTask(object state)
        {
            return CreateTask(state, VoidTaskResultType);
        }

        public static Task<T> CreateTask<T>(object state)
        {
            var task = new Task<T>(Generic<T>.NonExecutableFunction, state);
            TaskCapture.CaptureTask(task);
            return task;
        }

        public static Task CreateTask(object state, Type resultType)
        {
            if (resultType == null)
                throw new ArgumentNullException(nameof(resultType));

            if (ReferenceEquals(resultType, typeof(void)))
                resultType = VoidTaskResultType;

            var factoryMethod = _genericTaskFactoryMap.GetOrAdd(resultType, _createFactoryMethod);
            var task = factoryMethod(state);
            return task;
        }

        private static Func<Task, object> CompileGetContinuationObject()
        {
            var m_continuationObject = typeof(Task).GetField("m_continuationObject", BindingFlags.Instance | BindingFlags.NonPublic);
            if (m_continuationObject == null)
                throw new InvalidOperationException("Cannot find field 'm_continuationObject' in the 'Task' class.");

            var taskArg = Expression.Variable(typeof(Task), "task");
            var accessContinuation = Expression.MakeMemberAccess(taskArg, m_continuationObject);
            var lambda = Expression.Lambda(accessContinuation, taskArg);
            return (Func<Task, object>)lambda.Compile();
        }

        public static object GetContinuationObject(this Task task) => _getContinutationObjet(task);

        public static void ResetContinuation(this Task task)
        {
            var m_continuationObject = typeof(Task).GetField("m_continuationObject", BindingFlags.Instance | BindingFlags.NonPublic);
            m_continuationObject.SetValue(task, null);
        }

        public static bool TrySetException(this Task task, Exception ex)
        {
#warning pre-compile accessor for Task.TrySetException
            var method = task.GetType().GetMethod("TrySetException", BindingFlags.Instance | BindingFlags.NonPublic);
            return (bool)method.Invoke(task, new object[] { ex });
        }

        public static bool TrySetCanceled(this Task task)
        {
#warning pre-compile accessor for Task.TrySetCanceled
            var method = task.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(mi => mi.Name == "TrySetCanceled" && mi.GetParameters().Length == 1);
            return (bool)method.Invoke(task, new object[] { new CancellationToken(true) });
        }

        public static bool TrySetResult(this Task task, object result)
        {
#warning pre-compile accessor for Task.TrySetResult
#warning make sure that the result type matches
            var method = task.GetType().GetMethod("TrySetResult",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var taskResultType = task.GetResultType();
            if (taskResultType == VoidTaskResultType)
                result = null;

            // Quick Fix: JSON serializer deserializes integers as Long
            if (result != null && !taskResultType.IsAssignableFrom(result.GetType()))
                result = Convert.ChangeType(result, taskResultType);

            return (bool)method.Invoke(task, new[] { result });
        }

        public static Type GetResultType(this Task task)
        {
            return GetTaskResultType(task.GetType());
        }

        public static Type GetTaskResultType(Type taskType)
        {
            if (ReferenceEquals(taskType, typeof(Task)))
            {
                return VoidTaskResultType;
            }
            else if (taskType.IsGenericType() && taskType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return taskType.GetGenericArguments()[0];
            }
#warning temporary quick-fix. Still needed? WhenAllPromise : Task<VoidTaskResult>
            else if (taskType.FullName == "System.Threading.Tasks.Task+WhenAllPromise")
            {
                return VoidTaskResultType;
            }
#warning temporary quick-fix. Still needed? WhenAllPromise<T> : Task<T[]>
            else if (taskType.IsGenericType() && taskType.GetGenericTypeDefinition().FullName == "System.Threading.Tasks.Task+WhenAllPromise`1")
            {
                var elementType = taskType.GetGenericArguments()[0];
                return elementType.MakeArrayType();
            }
#warning temporary quick-fix. Still needed? DelayPromise : Task<VoidTaskResult>
            else if (taskType.FullName == "System.Threading.Tasks.Task+DelayPromise")
            {
                return VoidTaskResultType;
            }
            else
            {
                throw new Exception();
            }
        }

        public static bool IsVoidResult(Type taskType)
        {
            return ReferenceEquals(GetTaskResultType(taskType), VoidTaskResultType);
        }

        public static bool IsVoidResult(this Task task)
        {
            return IsVoidResult(task.GetType());
        }

        public static object GetResult(this Task task)
        {
            var taskType = task.GetType();

            if (ReferenceEquals(taskType, typeof(Task)))
            {
                return null;
            }
            else if (taskType.IsGenericType() && taskType.GetGenericTypeDefinition() == typeof(Task<>))
            {
#warning pre-compile accessor for Task.GetResult
                var pi = taskType.GetProperty(nameof(Task<object>.Result));
                var result = pi.GetValue(task);
                if (result != null && result.GetType() == VoidTaskResultType)
                    return null;
                return result;
            }
            else
            {
                throw new Exception();
            }
        }

        public static void SetStatus(this Task task, TaskStatus status)
        {
#warning pre-compile accessor for Task.SetStatus
            var statusFlags = ConvertTaskStatusToStateFlags(status);
            var m_stateFlags = typeof(Task).GetField("m_stateFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            var stateFlags = (int)m_stateFlags.GetValue(task);
            stateFlags = (stateFlags & -65208321) | statusFlags;
            m_stateFlags.SetValue(task, stateFlags);
        }

        public static void SetAsyncState(this Task task, object state)
        {
#warning pre-compile accessor for Task.SetAsyncState
            var m_stateObject = typeof(Task).GetField("m_stateObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            m_stateObject.SetValue(task, state);
        }

        public static Task<T> FromException<T>(Exception ex)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.TrySetException(ex);
            return tcs.Task;
        }

        public static Task FromException(Type taskResultType, Exception ex)
        {
#warning user proper implementation
            var task = CreateTask(null, taskResultType);
            task.TrySetException(ex);
            return task;
        }

        #region Internal implementation details

        private const string TaskStubCannotBeRunExceptionMessage =
            "Task created with TaskStub.Create method cannot be run. " +
            "Instead, it represents a pointer to a Dasync Routine, " +
            "similarly to a Task created with a TaskCompletionSource.";

        private static Task CreateGenericInternal<T>(object state) => CreateTask<T>(state);

        private static Func<object, Task> CreateFactoryMethod(Type returnType)
        {
            var method = _genericCreateMethod.MakeGenericMethod(returnType);
            return (Func<object, Task>)method.CreateDelegate(typeof(Func<object, Task>));
        }

        private static readonly Action<object> NonExecutableAction =
            state => throw new InvalidOperationException(TaskStubCannotBeRunExceptionMessage);

        private sealed class Generic<T>
        {
            public static readonly Func<object, T> NonExecutableFunction =
                state => throw new InvalidOperationException(TaskStubCannotBeRunExceptionMessage);
        }

#warning replace with regular dictionary and read-write lock
        private static readonly ConcurrentDictionary<Type, Func<object, Task>>
            _genericTaskFactoryMap = new ConcurrentDictionary<Type, Func<object, Task>>();

        private static readonly Func<Type, Func<object, Task>>
            _createFactoryMethod = CreateFactoryMethod;

        private static readonly MethodInfo _genericCreateMethod =
            typeof(TaskAccessor).GetMethod(
                nameof(CreateGenericInternal),
                BindingFlags.NonPublic | BindingFlags.Static);

        private static int ConvertTaskStatusToStateFlags(TaskStatus status)
        {
            switch (status)
            {
                case TaskStatus.Created:
                    return 0;

                case TaskStatus.WaitingToRun:
                    return 0x00010000;

                case TaskStatus.Running:
                    return 0x00020000;

                case TaskStatus.RanToCompletion:
                    return 0x01000000;

                case TaskStatus.Faulted:
                    return 0x00200000;

                case TaskStatus.Canceled:
                    return 0x00400000;

                case TaskStatus.WaitingForActivation:
                    return 0x02000000;

                case TaskStatus.WaitingForChildrenToComplete:
                    return 0x00800000;

                default:
                    throw new ArgumentException($"Cannot convert TaskStatus value '{status}' to state flags.");
            }
        }

#endregion
    }
}
