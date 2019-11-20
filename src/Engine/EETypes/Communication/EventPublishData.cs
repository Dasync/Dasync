using System.Collections.Generic;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Communication
{
    public class EventPublishData
    {
        public string IntentId { get; set; }

        public ServiceId Service { get; set; }

        public EventId Event { get; set; }

        public CallerDescriptor Caller { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }

        public IValueContainer Parameters { get; set; }
    }
}
