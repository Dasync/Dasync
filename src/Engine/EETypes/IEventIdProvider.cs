using System.Reflection;

namespace Dasync.EETypes
{
    public interface IEventIdProvider
    {
        EventId GetId(EventInfo eventInfo);
    }
}
