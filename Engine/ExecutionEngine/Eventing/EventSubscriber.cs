using System;
using System.Collections.Generic;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Eventing;

namespace Dasync.ExecutionEngine.Eventing
{
    public class EventSubscriber : IEventSubscriber
    {
        private readonly Dictionary<EventDescriptor, HashSet<EventSubscriberDescriptor>> _subscriberMap =
            new Dictionary<EventDescriptor, HashSet<EventSubscriberDescriptor>>();

        public void Subscribe(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber)
        {
            if (!_subscriberMap.TryGetValue(eventDesc, out var subscribers))
                _subscriberMap.Add(eventDesc, subscribers = new HashSet<EventSubscriberDescriptor>());
            subscribers.Add(subscriber);
        }

        public IEnumerable<EventSubscriberDescriptor> GetSubscribers(EventDescriptor eventDesc)
        {
            if (_subscriberMap.TryGetValue(eventDesc, out var subscribers))
                return subscribers;
            else
                return Array.Empty<EventSubscriberDescriptor>();
        }
    }



}
