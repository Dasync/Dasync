using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Descriptors
{
    public interface ITaskResult
    {
        object Value { get; }

        Exception Exception { get; }

        bool IsCanceled { get; }
    }

    internal interface IMutableTaskResult : ITaskResult
    {
        object Value { get; set; }

        Exception Exception { get; set; }

        bool IsCanceled { get; set; }
    }

    public static class TaskResultExtensions
    {
        /// <summary>
        /// When TRUE, the routine has failed with an error described in the
        /// <see cref="Exception"/> property. This flag is mutually exclusive
        /// with <see cref="ITaskResult.IsCanceled"/> and <see cref="IsSucceeded"/>.
        /// </summary>
        public static bool IsFaulted(this ITaskResult taskResult) => taskResult.Exception != null;

        /// <summary>
        /// When TRUE, the routine execution has succeeded, and optionally
        /// has a <see cref="ITaskResult.Value"/> if the routine method returns generic
        /// <see cref="Task{TResult}"/>. This flag is mutually exclusive with
        /// <see cref="IsFaulted"/> and <see cref="ITaskResult.IsCanceled"/>.
        /// </summary>
        public static bool IsSucceeded(this ITaskResult taskResult) => !taskResult.IsCanceled && !taskResult.IsFaulted();
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TaskResult : ITaskResult, IMutableTaskResult, IValueContainer, IValueContainerWithTypeInfo
    {
        public static ITaskResult Create(Type valueType, object value, Exception exception, bool isCanceled)
        {
            var taskResult = (IMutableTaskResult)CreateEmpty(valueType);
            taskResult.Value = value;
            taskResult.Exception = exception;
            taskResult.IsCanceled = isCanceled;
            return taskResult;
        }

        public static ITaskResult CreateEmpty(Type valueType)
        {
            if (valueType == null || valueType == typeof(void) || valueType == typeof(object))
            {
                return new TaskResult();
            }
            else
            {
                return (ITaskResult)Activator.CreateInstance(typeof(TaskResult<>).MakeGenericType(valueType));
            }
        }

        /// <summary>
        /// The value of the result if the <see cref="IsSucceeded"/>.
        /// Always NULL for a <see cref="Task"/>.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// An exception object if <see cref="IsFaulted"/>.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// When TRUE, the routine has been canceled by a <see cref="CancellationToken"/>
        /// that is associated with it. This flag is mutually exclusive with
        /// <see cref="IsSucceeded"/> and <see cref="IsFaulted"/>.
        /// </summary>
        public bool IsCanceled { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                if (IsCanceled)
                    return "😶";
                if (this.IsFaulted())
                    return "🙁 " + Exception.GetType().FullName;
                return "🙂 " + (Value == null ? "" : Value.ToString());
            }
        }

        Type IValueContainerWithTypeInfo.GetObjectType() => GetType();

        int IValueContainer.GetCount() => 3;

        string IValueContainer.GetName(int index)
        {
            switch (index)
            {
                case 0: return nameof(Value);
                case 1: return nameof(Exception);
                case 2: return nameof(IsCanceled);
                default: throw new IndexOutOfRangeException();
            }
        }

        Type IValueContainer.GetType(int index)
        {
            switch (index)
            {
                case 0: return typeof(object);
                case 1: return typeof(Exception);
                case 2: return typeof(bool);
                default: throw new IndexOutOfRangeException();
            }
        }

        object IValueContainer.GetValue(int index)
        {
            switch (index)
            {
                case 0: return Value;
                case 1: return Exception;
                case 2: return IsCanceled;
                default: throw new IndexOutOfRangeException();
            }
        }

        void IValueContainer.SetValue(int index, object value)
        {
            switch (index)
            {
                case 0: Value = value; break;
                case 1: Exception = (Exception)value; break;
                case 2: IsCanceled = (bool)value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TaskResult<T> : ITaskResult, IMutableTaskResult, IValueContainer, IValueContainerWithTypeInfo
    {
        /// <summary>
        /// The value of the result if the <see cref="IsSucceeded"/>.
        /// Always NULL for a <see cref="Task"/>.
        /// </summary>
        public T Value { get; set; }

        object ITaskResult.Value => Value;

        object IMutableTaskResult.Value
        {
            get => Value;
            set
            {
                if (value == null && !typeof(T).IsClass)
                {
                    Value = (T)Activator.CreateInstance(typeof(T));
                }
                else
                {
                    Value = (T)value;
                }
            }
        }

        /// <summary>
        /// An exception object if <see cref="IsFaulted"/>.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// When TRUE, the routine has been canceled by a <see cref="CancellationToken"/>
        /// that is associated with it. This flag is mutually exclusive with
        /// <see cref="IsSucceeded"/> and <see cref="IsFaulted"/>.
        /// </summary>
        public bool IsCanceled { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                if (IsCanceled)
                    return "😶";
                if (this.IsFaulted())
                    return "🙁 " + Exception.GetType().FullName;
                return "🙂 " + (Value == null ? "" : Value.ToString());
            }
        }

        Type IValueContainerWithTypeInfo.GetObjectType() => GetType();

        int IValueContainer.GetCount() => 3;

        string IValueContainer.GetName(int index)
        {
            switch (index)
            {
                case 0: return nameof(Value);
                case 1: return nameof(Exception);
                case 2: return nameof(IsCanceled);
                default: throw new IndexOutOfRangeException();
            }
        }

        Type IValueContainer.GetType(int index)
        {
            switch (index)
            {
                case 0: return typeof(T);
                case 1: return typeof(Exception);
                case 2: return typeof(bool);
                default: throw new IndexOutOfRangeException();
            }
        }

        object IValueContainer.GetValue(int index)
        {
            switch (index)
            {
                case 0: return Value;
                case 1: return Exception;
                case 2: return IsCanceled;
                default: throw new IndexOutOfRangeException();
            }
        }

        void IValueContainer.SetValue(int index, object value)
        {
            switch (index)
            {
                case 0: Value = (T)value; break;
                case 1: Exception = (Exception)value; break;
                case 2: IsCanceled = (bool)value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}
