namespace Dasync.EETypes.Communication
{
    public class EventCommunicationSettings
    {
        public string CommunicationType { get; set; }

        /// <summary>
        /// Deduplicate messages on receive when the receiving communication method supports
        /// such a feature, or if a UoW/cache mechanism is available in the app.
        /// </summary>
        public bool Deduplicate { get; set; }

        /// <summary>
        /// Prefer an event to be executed via a communication method that has a message delivery guarantee.
        /// For example, an HTTP invocation can delegate execution to an event stream.
        /// Resiliency cannot be guaranteed if the desired communication method does not support it.
        /// </summary>
        public bool Resilient { get; set; }

        /// <summary>
        /// Ignore the transaction even if the method that publishes this event is transctional.
        /// This allows better latency but no consistency.
        /// </summary>
        public bool IgnoreTransaction { get; set; }
    }
}
