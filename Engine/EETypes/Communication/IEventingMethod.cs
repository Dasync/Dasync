using Microsoft.Extensions.Configuration;

namespace Dasync.EETypes.Communication
{
    public interface IEventingMethod
    {
        string Type { get; }

        IEventPublisher CreateEventPublisher(IConfiguration configuration);
    }
}
