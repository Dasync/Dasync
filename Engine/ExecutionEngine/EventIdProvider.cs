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
                Name = eventInfo.Name
            };
        }
    }
}
