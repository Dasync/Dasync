using System.Threading.Tasks;

namespace Dasync.EETypes.Communication
{
    public interface IEventPublisher
    {
        string Type { get; }

        CommunicationTraits Traits { get; }

        Task PublishAsync(EventPublishData data, PublishPreferences preferences);
    }
}
