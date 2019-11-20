using System.Collections.Generic;
using Dasync.DependencyInjection;
using Dasync.EETypes;
using Dasync.EETypes.Cancellation;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Configuration;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Eventing;
using Dasync.EETypes.Ioc;
using Dasync.EETypes.Persistence;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Proxy;
using Dasync.EETypes.Resolvers;
using Dasync.EETypes.Triggers;
using Dasync.ExecutionEngine.Cancellation;
using Dasync.ExecutionEngine.Communication;
using Dasync.ExecutionEngine.Configuration;
using Dasync.ExecutionEngine.Continuation;
using Dasync.ExecutionEngine.Eventing;
using Dasync.ExecutionEngine.IntrinsicFlow;
using Dasync.ExecutionEngine.Modeling;
using Dasync.ExecutionEngine.Persistence;
using Dasync.ExecutionEngine.Proxy;
using Dasync.ExecutionEngine.Resolvers;
using Dasync.ExecutionEngine.Startup;
using Dasync.ExecutionEngine.StateMetadata.Service;
using Dasync.ExecutionEngine.Transitions;
using Dasync.ExecutionEngine.Triggers;
using Dasync.Modeling;
using Dasync.Proxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dasync.ExecutionEngine
{
    public static class DI
    {
        public static readonly IEnumerable<ServiceDescriptor> Bindings = new ServiceDescriptorList().Configure();

        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddSingleton<IHostedService, StartupHostedService>();
            services.AddSingleton<ICommunicationListener, CommunicationListener>();
            services.AddSingleton<IServiceProxyBuilder, ServiceProxyBuilder>();
            services.AddSingleton<IProxyMethodExecutor, ProxyMethodExecutor>();
            services.AddSingleton<ITaskContinuationTracker, TaskContinuationTracker>();
            services.AddSingleton<ITaskContinuationClassifier, TaskContinuationClassifier>();
            services.AddSingleton<ICancellationTokenSourceRegistry, CancellationTokenSourceRegistry>();
            services.AddSingleton<IMethodIdProvider, MethodIdProvider>();
            services.AddSingleton<IEventIdProvider, EventIdProvider>();
            services.AddSingleton<IIntrinsicFlowController, IntrinsicFlowController>();
            services.AddSingleton<ITransitionScope, TransitionScope>();
            services.AddSingleton<ITransitionMonitorFactory, TransitionMonitorFactory>();
            services.AddSingleton<TransitionRunner>();
            services.AddSingleton<ITransitionRunner>(_ => _.GetService<TransitionRunner>());
            services.AddSingleton<ILocalMethodRunner>(_ => _.GetService<TransitionRunner>());
            services.AddSingleton<IUniqueIdGenerator, UniqueIdGenerator>();
            services.AddSingleton<IServiceStateMetadataProvider, ServiceStateMetadataProvider>();
            services.AddSingleton<IServiceStateValueContainerProvider, ServiceStateValueContainerProvider>();
            services.AddSingleton<SerializedServiceProxyBuilderHolder>();
            services.AddSingleton<ISerializedServiceProxyBuilder>(_ => _.GetService<SerializedServiceProxyBuilderHolder>());
            services.AddSingleton<ITaskCompletionSourceRegistry, TaskCompletionSourceRegistry>();
            services.AddSingleton<IntrinsicRoutines>();
            services.AddSingleton<IServiceResolver, ServiceResolver>();
            services.AddSingleton<IMethodResolver, MethodResolver>();
            services.AddSingleton<IEventResolver, EventResolver>();
            services.AddSingleton<RoutineCompletionNotificationHub>();
            services.AddSingleton<IRoutineCompletionNotifier>(_ => _.GetService<RoutineCompletionNotificationHub>());
            services.AddSingleton<IRoutineCompletionSink>(_ => _.GetService<RoutineCompletionNotificationHub>());
            services.AddSingleton<IDomainServiceProvider, DomainServiceProvider>();
            services.AddSingleton<ICommunicatorProvider, CommunicatorProvider>();
            services.AddSingleton<ICommunicationSettingsProvider, CommunicationSettingsProvider>();
            services.AddSingleton<IEventSubscriber, EventSubscriber>();
            services.AddSingleton<ISingleMethodInvoker, SingleMethodInvoker>();
            services.AddSingleton<IMethodStateStorageProvider, MethodStateStorageProvider>();
            services.AddSingleton<IExternalCommunicationModel, ExternalCommunicationModel>();
            services.AddSingleton<IEventPublisherProvider, EventPublisherProvider>();
            services.AddSingleton<ISingleEventPublisher, SingleEventPublisher>();
            services.AddSingleton<ICommunicationModelConfiguration, CommunicationModelConfiguration>();
            return services;
        }
    }
}
