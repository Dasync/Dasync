using System.Reflection;
using Dasync.EETypes;

namespace Dasync.ExecutionEngine
{
    public class EventIdProvider : IEventIdProvider
    {
        public EventId GetId(EventInfo eventInfo)
        {
            return new EventId
            {
                EventName = eventInfo.Name
            };
        }
    }
}
