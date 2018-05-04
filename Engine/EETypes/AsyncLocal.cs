#if NETFX45
using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#endif
using System.Threading;

namespace Dasync.EETypes
{
#if !NETFX45
    public sealed class AsyncLocal<T>
    {
        private readonly System.Threading.AsyncLocal<T> _asyncLocal = new System.Threading.AsyncLocal<T>();

        /// <summary>
        /// Gets or set the value to flow with <see cref="ExecutionContext"/>.
        /// </summary>
        public T Value
        {
            get => _asyncLocal.Value;
            set => _asyncLocal.Value = value;
        }
    }
#else
    public sealed class AsyncLocal<T>
    {
        private readonly string _name;
        private readonly AsyncLocalWrapper<T> _wrapper;

        public AsyncLocal()
        {
            if (AsyncLocalWrapper.IsAvailable)
                _wrapper = AsyncLocalWrapper<T>.Create();
            else
                _name = Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Gets or set the value to flow with <see cref="ExecutionContext"/>.
        /// </summary>
        public T Value
        {
            get
            {
                if (_wrapper != null)
                    return _wrapper.Value;

                var handle = CallContext.LogicalGetData(_name) as ObjectHandle;
                if (handle == null)
                    return default(T);
                return (T)handle.Unwrap();
            }
            set
            {
                if (_wrapper != null)
                {
                    _wrapper.Value = value;
                    return;
                }

                // Mimic the implementation of AsyncLocal<T>
                var executionContext = Thread.CurrentThread.GetMutableExecutionContext();
                var logicalCallContext = executionContext.GetLogicalCallContext();
                var datastore = logicalCallContext.GetDatastore();
                var datastoreCopy = datastore == null ? new Hashtable() : new Hashtable(datastore);
                datastoreCopy[_name] = new ObjectHandle(value);
                logicalCallContext.SetDatastore(datastoreCopy);
            }
        }
    }

    internal static class ExecutionContextExtensions
    {
        private static readonly Func<ExecutionContext, LogicalCallContext> _getLogicalCallContextFunc;

        static ExecutionContextExtensions()
        {
            var logicalCallContextPropertyInfo = typeof(ExecutionContext).GetProperty("LogicalCallContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var instanceParameterExpression = Expression.Parameter(typeof(ExecutionContext));
            var memberAccessExpression = Expression.MakeMemberAccess(instanceParameterExpression, logicalCallContextPropertyInfo);
            var lambdaExpression = Expression.Lambda<Func<ExecutionContext, LogicalCallContext>>(memberAccessExpression, instanceParameterExpression);
            _getLogicalCallContextFunc = lambdaExpression.Compile();
        }

        public static LogicalCallContext GetLogicalCallContext(this ExecutionContext executionContext)
            => _getLogicalCallContextFunc(executionContext);
    }

    internal static class LogicalCallContextExtensions
    {
        private static readonly Func<LogicalCallContext, Hashtable> _getDatastoreFunc;
        private static readonly Func<LogicalCallContext, Hashtable, bool> _setDatastoreFunc;

        static LogicalCallContextExtensions()
        {
            var datastoreFieldInfo = typeof(LogicalCallContext).GetField("m_Datastore", BindingFlags.Instance | BindingFlags.NonPublic);
            var instanceParameterExpression = Expression.Parameter(typeof(LogicalCallContext));
            var memberAccessExpression = Expression.MakeMemberAccess(instanceParameterExpression, datastoreFieldInfo);
            var getLambdaExpression = Expression.Lambda<Func<LogicalCallContext, Hashtable>>(memberAccessExpression, instanceParameterExpression);
            _getDatastoreFunc = getLambdaExpression.Compile();
            var valueParameterExpression = Expression.Parameter(typeof(Hashtable));
            var assignmentExpression = Expression.Assign(memberAccessExpression, valueParameterExpression);
            var setFunctionBody = Expression.Block(assignmentExpression, Expression.Constant(true));
            var setLambdaExpression = Expression.Lambda<Func<LogicalCallContext, Hashtable, bool>>(setFunctionBody, instanceParameterExpression, valueParameterExpression);
            _setDatastoreFunc = setLambdaExpression.Compile();
        }

        public static Hashtable GetDatastore(this LogicalCallContext context)
            => _getDatastoreFunc(context);

        public static void SetDatastore(this LogicalCallContext context, Hashtable datastore)
            => _setDatastoreFunc(context, datastore);
    }

    internal static class ThreadExtensions
    {
        private static readonly Func<Thread, ExecutionContext> _getMutableExecutionContextFunc;

        static ThreadExtensions()
        {
            var getMutableExecutionContextMethodInfo = typeof(Thread).GetMethod("GetMutableExecutionContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var instanceParameterExpression = Expression.Parameter(typeof(Thread));
            var functionCallExpression = Expression.Call(instanceParameterExpression, getMutableExecutionContextMethodInfo);
            var lambdaExpression = Expression.Lambda<Func<Thread, ExecutionContext>>(functionCallExpression, instanceParameterExpression);
            _getMutableExecutionContextFunc = lambdaExpression.Compile();
        }

        private static MethodInfo GetMutableExecutionContextMethodInfo =
            typeof(Thread).GetMethod("GetMutableExecutionContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static ExecutionContext GetMutableExecutionContext(this Thread thread)
            => _getMutableExecutionContextFunc(thread);
    }

    internal abstract class AsyncLocalWrapper
    {
        protected static Type AsyncLocalGenericType;

        static AsyncLocalWrapper()
        {
            AsyncLocalGenericType = typeof(Thread).Assembly.GetType("System.Threading.AsyncLocal`1", throwOnError: false);
        }

        public static bool IsAvailable => AsyncLocalGenericType != null;

        public static AsyncLocalWrapper<T> Create<T>() => AsyncLocalWrapper<T>.Create();
    }

    internal sealed class AsyncLocalWrapper<T> : AsyncLocalWrapper
    {
        private static Type AsyncLocalType;
        private static PropertyInfo ValueProperty;

        private object _asyncLocal;

        static AsyncLocalWrapper()
        {
            if (AsyncLocalGenericType != null)
            {
                AsyncLocalType = AsyncLocalGenericType.MakeGenericType(typeof(T));
                ValueProperty = AsyncLocalType.GetProperty("Value");
            }
        }

        private AsyncLocalWrapper()
        {
            _asyncLocal = Activator.CreateInstance(AsyncLocalType);
        }

        public static AsyncLocalWrapper<T> Create()
        {
            if (!IsAvailable)
                throw new InvalidOperationException();
            return new AsyncLocalWrapper<T>();
        }

        public T Value
        {
            get
            {
                return (T)ValueProperty.GetValue(_asyncLocal);
            }
            set
            {
                ValueProperty.SetValue(_asyncLocal, value);
            }
        }
    }
#endif
}
