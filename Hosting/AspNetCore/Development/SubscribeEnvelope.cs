using System.Collections.Generic;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;

namespace Dasync.Hosting.AspNetCore.Development
{
    public class SubscribeEnvelope
    {
        public ServiceId Service { get; set; }

        public EventId Event { get; set; }

        public List<EventSubscriberDescriptor> Subscribers { get; set; }
    }
}
