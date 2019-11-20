using System;
using System.Linq;
using System.Reflection;

namespace Dasync.Modeling
{
    internal class EventDefinition :
        PropertyBag, IMutablePropertyBag, IPropertyBag,
        IMutableEventDefinition, IEventDefinition
    {
        private EventInfo[] _interfaceEvents = Array.Empty<EventInfo>();

        public EventDefinition(ServiceDefinition serviceDefinition, EventInfo eventInfo)
        {
            ServiceDefinition = serviceDefinition;
            EventInfo = eventInfo;
            Name = eventInfo.Name;
        }

        public ServiceDefinition ServiceDefinition { get; }

        public string Name { get; }

        public EventInfo EventInfo { get; }

        public bool IsIgnored { get; set; }

        public EventInfo[] InterfaceEvents => _interfaceEvents;

        IServiceDefinition IEventDefinition.Service => ServiceDefinition;

        IMutableServiceDefinition IMutableEventDefinition.Service => ServiceDefinition;

        public void AddInterfaceEvent(EventInfo eventInfo)
        {
            if (_interfaceEvents.Contains(eventInfo))
                return;
            _interfaceEvents = _interfaceEvents.Concat(new[] { eventInfo }).ToArray();
        }
    }
}
