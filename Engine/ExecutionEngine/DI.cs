using System;
using System.Collections.Generic;
using Dasync.EETypes;
using Dasync.EETypes.Cancellation;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Ioc;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Proxy;
using Dasync.EETypes.Resolvers;
using Dasync.EETypes.Triggers;
using Dasync.ExecutionEngine.Cancellation;
using Dasync.ExecutionEngine.Communication;
using Dasync.ExecutionEngine.Continuation;
using Dasync.ExecutionEngine.Events;
using Dasync.ExecutionEngine.Extensions;
using Dasync.ExecutionEngine.IntrinsicFlow;
using Dasync.ExecutionEngine.Proxy;
using Dasync.ExecutionEngine.Resolvers;
using Dasync.ExecutionEngine.StateMetadata.Service;
using Dasync.ExecutionEngine.Transitions;
using Dasync.ExecutionEngine.Triggers;
using Dasync.Modeling;
using Dasync.Proxy;
using Microsoft.Extensions.Hosting;

namespace Dasync.ExecutionEngine
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IHostedService)] = typeof(StartupHostedService),
            [typeof(IServiceProxyBuilder)] = typeof(ServiceProxyBuilder),
            [typeof(IProxyMethodExecutor)] = typeof(ProxyMethodExecutor),
            [typeof(ITaskContinuationTracker)] = typeof(TaskContinuationTracker),
            [typeof(ITaskContinuationClassifier)] = typeof(TaskContinuationClassifier),
            [typeof(ICancellationTokenSourceRegistry)] = typeof(CancellationTokenSourceRegistry),
            [typeof(IMethodIdProvider)] = typeof(MethodIdProvider),
            [typeof(IEventIdProvider)] = typeof(EventIdProvider),
            [typeof(IIntrinsicFlowController)] = typeof(IntrinsicFlowController),
            [typeof(ITransitionScope)] = typeof(TransitionScope),
            [typeof(ITransitionMonitorFactory)] = typeof(TransitionMonitorFactory),
            [typeof(TransitionRunner)] = typeof(TransitionRunner),
            [typeof(ITransitionRunner)] = typeof(TransitionRunner),
            [typeof(ILocalMethodRunner)] = typeof(TransitionRunner),
            [typeof(ITransitionScope)] = typeof(TransitionScope),
            [typeof(IUniqueIdGenerator)] = typeof(UniqueIdGenerator),
            [typeof(IServiceStateMetadataProvider)] = typeof(ServiceStateMetadataProvider),
            [typeof(IServiceStateValueContainerProvider)] = typeof(ServiceStateValueContainerProvider),
            [typeof(ISerializedServiceProxyBuilder)] = typeof(SerializedServiceProxyBuilderHolder),
            [typeof(SerializedServiceProxyBuilderHolder)] = typeof(SerializedServiceProxyBuilderHolder),
            [typeof(ITaskCompletionSourceRegistry)] = typeof(TaskCompletionSourceRegistry),
            [typeof(IntrinsicRoutines)] = typeof(IntrinsicRoutines),
            [typeof(ITaskResultConverter)] = typeof(TaskResultConverter),
            [typeof(IServiceResolver)] = typeof(ServiceResolver),
            [typeof(IMethodResolver)] = typeof(MethodResolver),
            [typeof(IEventResolver)] = typeof(EventResolver),
            [typeof(RoutineCompletionNotificationHub)] = typeof(RoutineCompletionNotificationHub),
            [typeof(IRoutineCompletionNotifier)] = typeof(RoutineCompletionNotificationHub),
            [typeof(IRoutineCompletionSink)] = typeof(RoutineCompletionNotificationHub),
            [typeof(IDomainServiceProvider)] = typeof(DomainServiceProvider),
            [typeof(ICommunicatorProvider)] = typeof(CommunicatorProvider),
            [typeof(ICommunicationModelEnricher)] = typeof(CommunicationModelEnricher),
            [typeof(ICommunicationSettingsProvider)] = typeof(CommunicationSettingsProvider),
            [typeof(IEventSubscriber)] = typeof(EventSubscriber),
            [typeof(ISingleMethodInvoker)] = typeof(SingleMethodInvoker),
        };
    }
}
