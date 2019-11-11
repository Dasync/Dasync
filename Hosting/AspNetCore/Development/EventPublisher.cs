using System.Linq;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Eventing;
using Dasync.EETypes.Resolvers;
using Dasync.Hosting.AspNetCore.Invocation;

namespace Dasync.Hosting.AspNetCore.Development
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IEventSubscriber _eventSubscriber;
        private readonly IUniqueIdGenerator _idGenerator;
        private readonly ICommunicatorProvider _communicatorProvider;
        private readonly IServiceResolver _serviceResolver;
        private readonly ILocalMethodRunner _localMethodRunner;

        public EventPublisher(
            IEventSubscriber eventSubscriber,
            IUniqueIdGenerator idGenerator,
            ICommunicatorProvider communicatorProvider,
            IServiceResolver serviceResolver,
            ILocalMethodRunner localMethodRunner)
        {
            _eventSubscriber = eventSubscriber;
            _idGenerator = idGenerator;
            _communicatorProvider = communicatorProvider;
            _serviceResolver = serviceResolver;
            _localMethodRunner = localMethodRunner;
        }

        public string Type => EventingMethod.MethodType;

        public CommunicationTraits Traits => CommunicationTraits.Volatile;

        public async Task PublishAsync(EventPublishData data, PublishPreferences preferences)
        {
            var eventDesc = new EventDescriptor { Service = data.Service, Event = data.Event };
            var subscribers = _eventSubscriber.GetSubscribers(eventDesc).ToList();

            if (subscribers.Count == 0)
                return;

            foreach (var subscriber in subscribers)
            {
                var invokeData = new MethodInvocationData
                {
                    IntentId = _idGenerator.NewId(),
                    Service = subscriber.Service,
                    Method = subscriber.Method,
                    Parameters = data.Parameters,
                    FlowContext = data.FlowContext,
                    Caller = new CallerDescriptor(data.Service, data.Event, data.IntentId)
                };

                InvokeInBackground(invokeData, preferences);
            }
        }

        private async void InvokeInBackground(MethodInvocationData data, PublishPreferences preferences)
        {
            if (!_serviceResolver.TryResolve(data.Service, out var service) ||
                service.Definition.Type == Modeling.ServiceType.External)
            {
                var communicator = _communicatorProvider.GetCommunicator(data.Service, data.Method, assumeExternal: true);
                await communicator.InvokeAsync(data, default);
            }
            else if (!preferences.SkipLocalSubscribers)
            {
                var message = new HttpCommunicatorMessage { IsRetry = false };
                await _localMethodRunner.RunAsync(data, message);
            }
        }
    }
}
