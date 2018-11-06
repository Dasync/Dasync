using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Platform
{
    public interface IEventSubscriber
    {
        void Subscribe(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber);
    }
}
