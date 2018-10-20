using System;
using System.Collections.Generic;
using Dasync.EETypes;
using Dasync.EETypes.Cancellation;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Proxy;
using Dasync.ExecutionEngine.Cancellation;
using Dasync.ExecutionEngine.Continuation;
using Dasync.ExecutionEngine.Intents;
using Dasync.ExecutionEngine.IntrinsicFlow;
using Dasync.ExecutionEngine.Proxy;
using Dasync.ExecutionEngine.StateMetadata.Service;
using Dasync.ExecutionEngine.Transitions;
using Dasync.Proxy;

namespace Dasync.ExecutionEngine
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IServiceProxyBuilder)] = typeof(ServiceProxyBuilder),
            [typeof(IProxyMethodExecutor)] = typeof(ProxyMethodExecutor),
            [typeof(ITaskContinuationTracker)] = typeof(TaskContinuationTracker),
            [typeof(ITaskContinuationClassifier)] = typeof(TaskContinuationClassifier),
            [typeof(ICancellationTokenSourceRegistry)] = typeof(CancellationTokenSourceRegistry),
            [typeof(ICancellationTokenSourceIdGenerator)] = typeof(CancellationTokenSourceIdGenerator),
            [typeof(IRoutineMethodIdProvider)] = typeof(RoutineMethodIdProvider),
            [typeof(IIntrinsicFlowController)] = typeof(IntrinsicFlowController),
            [typeof(ITransitionScope)] = typeof(TransitionScope),
            [typeof(ITransitionMonitorFactory)] = typeof(TransitionMonitorFactory),
            [typeof(ITransitionRunner)] = typeof(TransitionRunner),
            [typeof(ITransitionScope)] = typeof(TransitionScope),
            [typeof(INumericIdGenerator)] = typeof(NumericIdGenerator),
            [typeof(IRoutineMethodResolver)] = typeof(RoutineMethodResolver),
            [typeof(IServiceStateMetadataProvider)] = typeof(ServiceStateMetadataProvider),
            [typeof(IServiceStateValueContainerProvider)] = typeof(ServiceStateValueContainerProvider),
            [typeof(ISerializedServiceProxyBuilder)] = typeof(SerializedServiceProxyBuilderHolder),
            [typeof(SerializedServiceProxyBuilderHolder)] = typeof(SerializedServiceProxyBuilderHolder)
        };
    }
}
