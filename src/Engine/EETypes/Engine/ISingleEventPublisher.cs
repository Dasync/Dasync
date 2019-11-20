using System.Threading.Tasks;
using Dasync.EETypes.Intents;

namespace Dasync.EETypes.Engine
{
    public interface ISingleEventPublisher
    {
        Task PublishAsync(RaiseEventIntent intent);
    }
}
