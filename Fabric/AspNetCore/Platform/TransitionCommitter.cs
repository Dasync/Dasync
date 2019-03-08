using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AspNetCore.Communication;
using Dasync.EETypes;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Platform;
using Dasync.Modeling;

namespace Dasync.AspNetCore.Platform
{
    public class TransitionCommitter : ITransitionCommitter
    {
        private readonly ICommunicationModelProvider _communicationModelProvider;
        private readonly IPlatformHttpClientProvider _platformHttpClientProvider;
        private readonly IRoutineCompletionSink _routineCompletionSink;
        private readonly IEventDispatcher _eventDispatcher;

        public TransitionCommitter(
            ICommunicationModelProvider communicationModelProvider,
            IPlatformHttpClientProvider platformHttpClientProvider,
            IRoutineCompletionSink routineCompletionSink,
            IEventDispatcher eventDispatcher)
        {
            _communicationModelProvider = communicationModelProvider;
            _platformHttpClientProvider = platformHttpClientProvider;
            _routineCompletionSink = routineCompletionSink;
            _eventDispatcher = eventDispatcher;
        }

        public async Task CommitAsync(ScheduledActions actions, ITransitionCarrier transitionCarrier, TransitionCommitOptions options, CancellationToken ct)
        {
            if (actions.ExecuteRoutineIntents?.Count > 0)
            {
                foreach (var intent in actions.ExecuteRoutineIntents)
                {
                    var serviceDefinition = GetServiceDefinition(intent.ServiceId);
                    var platformHttpClient = _platformHttpClientProvider.GetClient(serviceDefinition);

                    var routineInfo = await platformHttpClient.ScheduleRoutineAsync(intent, ct);

                    if (options.NotifyOnRoutineCompletion && routineInfo.Result != null)
                        _routineCompletionSink.OnRoutineCompleted(intent.Id, routineInfo.Result);
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

        private IServiceDefinition GetServiceDefinition(ServiceId serviceId)
        {
            var serviceName = serviceId.ProxyName ?? serviceId.ServiceName;

            var serviceDefinition = _communicationModelProvider.Model.Services.FirstOrDefault(d => d.Name == serviceName);
            if (serviceDefinition == null)
                throw new ArgumentException($"Service '{serviceName}' is not registered.");

            return serviceDefinition;
        }
    }
}
