using Dasync.EETypes.Descriptors;
using System.Collections.Generic;

namespace Dasync.EETypes.Eventing
{
    public interface IEventSubscriber
    {
        void Subscribe(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber);

        IEnumerable<EventDescriptor> SubscribedEvents { get; }

        IEnumerable<EventSubscriberDescriptor> GetSubscribers(EventDescriptor eventDesc);
    }
}
