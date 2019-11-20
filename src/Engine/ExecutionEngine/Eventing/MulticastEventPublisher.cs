using System;
using System.Threading.Tasks;
using Dasync.EETypes.Communication;

namespace Dasync.ExecutionEngine.Eventing
{
    public class MulticastEventPublisher : IEventPublisher, IDisposable
    {
        public MulticastEventPublisher(IEventPublisher localPublisher, IEventPublisher externalPublisher)
        {
            LocalPublisher = localPublisher;
            ExternalPublisher = externalPublisher;
        }

        public IEventPublisher LocalPublisher { get; }

        public IEventPublisher ExternalPublisher { get; }

        public string Type => "multicast";

        public CommunicationTraits Traits => default;

        public async Task PublishAsync(EventPublishData data, PublishPreferences preferences)
        {
            await LocalPublisher.PublishAsync(data, default);
            await ExternalPublisher.PublishAsync(data, new PublishPreferences { SkipLocalSubscribers = true });
        }

        public void Dispose()
        {
            (LocalPublisher as IDisposable)?.Dispose();
            (ExternalPublisher as IDisposable)?.Dispose();
        }
    }
}
