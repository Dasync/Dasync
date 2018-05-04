using System.Reflection;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Proxy;
using Dasync.ExecutionEngine.Transitions;
using Dasync.Proxy;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.Proxy
{
    public class ProxyMethodExecutor : IProxyMethodExecutor
    {
        private readonly ITransitionScope _transitionScope;
        private readonly IRoutineMethodIdProvider _routineMethodIdProvider;
        private readonly INumericIdGenerator _numericIdGenerator;
        private readonly IRoutineImmediateExecutor _routineImmediateExecutor;

        public ProxyMethodExecutor(
            ITransitionScope transitionScope,
            IRoutineMethodIdProvider routineMethodIdProvider,
            INumericIdGenerator numericIdGenerator,
            IRoutineImmediateExecutor routineImmediateExecutor)
        {
            _transitionScope = transitionScope;
            _routineMethodIdProvider = routineMethodIdProvider;
            _numericIdGenerator = numericIdGenerator;
            _routineImmediateExecutor = routineImmediateExecutor;
        }

        public Task Execute<TParameters>(IProxy proxy, MethodInfo methodInfo, ref TParameters parameters)
            where TParameters : IValueContainer
        {
            var serviceProxyContext = (ServiceProxyContext)proxy.Context;

            var intent = new ExecuteRoutineIntent
            {
                Id = _numericIdGenerator.NewId(),
                ServiceId = serviceProxyContext.Service.Id,
                MethodId = _routineMethodIdProvider.GetId(methodInfo),
                Parameters = parameters
            };

            var taskResultType =
                // Dispose() does not return a task, and is the only exception.
#warning check if it's really IDisposable.Dispose
                methodInfo.ReturnType == typeof(void)
                ? TaskAccessor.VoidTaskResultType
                : TaskAccessor.GetTaskResultType(methodInfo.ReturnType);

            var taskState = new ProxyTaskState
            {
                IntentId = intent.Id
#warning must have id of actual routine for dynamic subscription (subscribe after a routine already scheduled).
            };

            var proxyTask = TaskAccessor.CreateTask(taskState, taskResultType);

            if (_transitionScope.IsActive)
            {
                _transitionScope.CurrentMonitor.RegisterIntent(intent, proxyTask);
            }
            else
            {
                _routineImmediateExecutor.ExecuteAndAwaitInBackground(intent, proxyTask);
            }

            return proxyTask;
        }
    }
}
