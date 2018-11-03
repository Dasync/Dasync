using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Platform;

namespace Dasync.Fabric.Sample.Base
{
    public class EventSubscriber : IEventSubscriber
    {
        private readonly IFabricConnectorSelector _fabricConnectorSelector;

        public EventSubscriber(IFabricConnectorSelector fabricConnectorSelector)
        {
            _fabricConnectorSelector = fabricConnectorSelector;
        }

        public async void Subscribe(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber)
        {
            var fabricConnectorToSubscriber = _fabricConnectorSelector.Select(subscriber.ServiceId);
            await fabricConnectorToSubscriber.SubscribeForEventAsync(eventDesc, subscriber);

            var fabricConnectorToPublisher = _fabricConnectorSelector.Select(eventDesc.ServiceId);
            await fabricConnectorToPublisher.OnEventSubscriberAddedAsync(eventDesc, subscriber);
        }
    }
}
