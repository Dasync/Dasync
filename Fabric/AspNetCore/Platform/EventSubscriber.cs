using System;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Platform;

namespace Dasync.AspNetCore.Platform
{
    public class EventSubscriber : IEventSubscriber
    {
        public void Subscribe(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber)
        {
            throw new NotImplementedException();
        }
    }
}
