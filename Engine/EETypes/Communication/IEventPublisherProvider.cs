namespace Dasync.EETypes.Communication
{
    public interface IEventPublisherProvider
    {
        IEventPublisher GetPublisher(ServiceId serviceId, EventId eventId);
    }
}
