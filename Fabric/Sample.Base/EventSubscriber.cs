using System;
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
            var fabricConnectorToSubscriber = _fabricConnectorSelector.Select(subscriber.Service);
            var fabricConnectorToPublisher = _fabricConnectorSelector.Select(eventDesc.Service);

            if (fabricConnectorToSubscriber.GetType() != fabricConnectorToPublisher.GetType())
                throw new NotSupportedException("Multi-type fabric is not supported for events, because it's an infrastructure configuration concern.");

            await fabricConnectorToSubscriber.SubscribeForEventAsync(eventDesc, subscriber, fabricConnectorToPublisher);
            await fabricConnectorToPublisher.OnEventSubscriberAddedAsync(eventDesc, subscriber, fabricConnectorToSubscriber);
        }
    }
}
