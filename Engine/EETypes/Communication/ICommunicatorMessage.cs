namespace Dasync.EETypes.Communication
{
    public interface ICommunicatorMessage
    {
        string CommunicatorType { get; }

        CommunicationTraits CommunicatorTraits { get; }

        /// <summary>
        /// NULL when no such info is available.
        /// </summary>
        bool? IsRetry { get; }

        /// <summary>
        /// Optional request/message ID (should be unique) specified by the caller.
        /// </summary>
        string RequestId { get; }
    }
}
