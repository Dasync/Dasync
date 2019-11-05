using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Intents;
using Dasync.ExecutionEngine.Transitions;

namespace Dasync.ExecutionEngine.Communication
{
    public class SingleEventPublisher : ISingleEventPublisher
    {
        private readonly ITransitionScope _transitionScope;
        private readonly IEventPublisherProvider _eventPublisherProvider;

        public SingleEventPublisher(
            ITransitionScope transitionScope,
            IEventPublisherProvider eventPublisherProvider)
        {
            _transitionScope = transitionScope;
            _eventPublisherProvider = eventPublisherProvider;
        }

        public async Task PublishAsync(RaiseEventIntent intent)
        {
            var eventData = new EventPublishData
            {
                IntentId = intent.Id,
                Service = intent.Service,
                Event = intent.Event,
                Parameters = intent.Parameters
            };

            if (_transitionScope.IsActive)
            {
                var context = (ITransitionContext)_transitionScope.CurrentMonitor.Context;
                eventData.FlowContext = context.FlowContext;
                eventData.Caller = context.CurrentAsCaller();
            }

            var publisher = _eventPublisherProvider.GetPublisher(intent.Service, intent.Event);
            await publisher.PublishAsync(eventData);
        }
    }
}
