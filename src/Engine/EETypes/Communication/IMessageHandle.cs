using System.Threading.Tasks;

namespace Dasync.EETypes.Communication
{
    public interface IMessageHandle
    {
        /// <summary>
        /// An internal ID of the message. For informational purposes only.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Completes the message that no other consumer can receive it.
        /// </summary>
        Task Complete();

        /// <summary>
        /// Releases the message lock so it can picked up by another consumer possibly with a delay.
        /// </summary>
        void ReleaseLock();
    }
}
