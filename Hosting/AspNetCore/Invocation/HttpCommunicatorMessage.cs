using Dasync.EETypes.Communication;

namespace Dasync.Hosting.AspNetCore.Invocation
{
    public class HttpCommunicatorMessage : ICommunicatorMessage
    {
        public string CommunicatorType => "http";

        public CommunicationTraits CommunicatorTraits =>
            CommunicationTraits.Volatile |
            (WaitForResult ? CommunicationTraits.SyncReplies : default);

        public bool? IsRetry { get; set; }

        public string RequestId { get; set; }

        public bool WaitForResult { get; set; }
    }
}
