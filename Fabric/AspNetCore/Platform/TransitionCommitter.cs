using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AspNetCore.Communication;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Ioc;
using Dasync.EETypes.Platform;
using Dasync.ExecutionEngine;
using Dasync.ExecutionEngine.Extensions;
using Dasync.Modeling;
using Dasync.Proxy;

namespace Dasync.AspNetCore.Platform
{
    public class TransitionCommitter : ITransitionCommitter
    {
        private readonly ICommunicationModel _communicationModel;
        private readonly IPlatformHttpClientProvider _platformHttpClientProvider;
        private readonly IRoutineCompletionSink _routineCompletionSink;
        private readonly IEventDispatcher _eventDispatcher;
        private readonly IRoutineMethodResolver _routineMethodResolver;
        private readonly IDomainServiceProvider _domainServiceProvider;
        private readonly IMethodInvokerFactory _methodInvokerFactory;
        private readonly IEnumerable<IRoutineTransitionAction> _transitionActions;
        private readonly ITransitionUserContext _transitionUserContext;

        public TransitionCommitter(
            ICommunicationModel communicationModel,
            IPlatformHttpClientProvider platformHttpClientProvider,
            IRoutineCompletionSink routineCompletionSink,
            IEventDispatcher eventDispatcher,
            IRoutineMethodResolver routineMethodResolver,
            IDomainServiceProvider domainServiceProvider,
            IMethodInvokerFactory methodInvokerFactory,
            IEnumerable<IRoutineTransitionAction> transitionActions,
            ITransitionUserContext transitionUserContext)
        {
            _communicationModel = communicationModel;
            _platformHttpClientProvider = platformHttpClientProvider;
            _routineCompletionSink = routineCompletionSink;
            _eventDispatcher = eventDispatcher;
            _routineMethodResolver = routineMethodResolver;
            _domainServiceProvider = domainServiceProvider;
            _methodInvokerFactory = methodInvokerFactory;
            _transitionActions = transitionActions;
            _transitionUserContext = transitionUserContext;
        }

        public async Task CommitAsync(ScheduledActions actions, ITransitionCarrier transitionCarrier, TransitionCommitOptions options, CancellationToken ct)
        {
            if (actions.ExecuteRoutineIntents?.Count > 0)
            {
                foreach (var intent in actions.ExecuteRoutineIntents)
                {
                    var serviceDefinition = GetServiceDefinition(intent.ServiceId);

                    if (serviceDefinition.Type == ServiceType.Local)
                    {
#pragma warning disable CS4014
                        Task.Run(() => RunRoutineInBackground(serviceDefinition, intent));
#pragma warning restore CS4014
                    }
                    else
                    {
                        var platformHttpClient = _platformHttpClientProvider.GetClient(serviceDefinition);
                        var routineInfo = await platformHttpClient.ScheduleRoutineAsync(intent, _transitionUserContext.Current, ct);
                        if (routineInfo.Result != null)
                            _routineCompletionSink.OnRoutineCompleted(intent.Id, routineInfo.Result);
                    }
                }
            }

            if (actions.RaiseEventIntents?.Count > 0)
            {
                foreach (var intent in actions.RaiseEventIntents)
                {
                    await _eventDispatcher.PublishEvent(intent);
                }
            }
        }

        private async void RunRoutineInBackground(IServiceDefinition serviceDefinition, ExecuteRoutineIntent intent)
        {
            try
            {
                var serviceInstance = _domainServiceProvider.GetService(serviceDefinition.Implementation);
                var routineMethod = _routineMethodResolver.Resolve(serviceDefinition, intent.MethodId);
                var methodInvoker = _methodInvokerFactory.Create(routineMethod);

                foreach (var postAction in _transitionActions)
                    await postAction.OnRoutineStartAsync(serviceDefinition, intent.ServiceId, intent.MethodId, intent.Id);

                Task task;
                try
                {
                    task = methodInvoker.Invoke(serviceInstance, intent.Parameters);
                    if (routineMethod.ReturnType != typeof(void))
                    {
                        try { await task; } catch { }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException)
                        ex = ex.InnerException;
                    task = Task.FromException(ex);
                }
                var taskResult = task?.ToTaskResult() ?? new TaskResult();

                foreach (var postAction in _transitionActions)
                    await postAction.OnRoutineCompleteAsync(serviceDefinition, intent.ServiceId, intent.MethodId, intent.Id, taskResult);

                _routineCompletionSink.OnRoutineCompleted(intent.Id, taskResult);
            }
            catch
            {
            }
        }

        private IServiceDefinition GetServiceDefinition(ServiceId serviceId)
        {
            var serviceName = serviceId.ProxyName ?? serviceId.ServiceName;

            var serviceDefinition = _communicationModel.Services.FirstOrDefault(d => d.Name == serviceName);
            if (serviceDefinition == null)
                throw new ArgumentException($"Service '{serviceName}' is not registered.");

            return serviceDefinition;
        }
    }
}
