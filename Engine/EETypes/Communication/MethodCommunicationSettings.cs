namespace Dasync.EETypes.Communication
{
    public class MethodCommunicationSettings
    {
        public string CommunicationType { get; set; }

        /// <summary>
        /// Deduplicate messages on receive when the receiving communication method supports
        /// such a feature, or if a UoW/cache mechanism is available in the app.
        /// </summary>
        public bool Deduplicate { get; set; }

        /// <summary>
        /// Prefer a method to be executed via a communication method that has a message delivery guarantee.
        /// For example, an HTTP invocation can delegate execution to a message queue.
        /// Resiliency cannot be guaranteed if the desired communication method does not support it.
        /// </summary>
        public bool Resilient { get; set; }

        /// <summary>
        /// Method state should be saved and restored when sending commands (sometimes queries).
        /// If FALSE, then wait for the command completion in process.
        /// Persistence won't be available if there are no meachnisms registered in an app,
        /// unless <see cref="RoamingState"/> is set to TRUE.
        /// </summary>
        public bool Persistent { get; set; }

        /// <summary>
        /// When <see cref="Persistent"/> is enabled, convey the state of a method inside a command,
        /// so the state is restored from the response instead of a persistence mechanism.
        /// Does not work with <see cref="Task.WhenAll"/> because it requires concurrency check.
        /// Has no effect if a command (sometimes queries) has no continuation.
        /// </summary>
        public bool RoamingState { get; set; }

        /// <summary>
        /// Enables Unit of Work - send all commands/events and save DB entities at once
        /// when a method transition completes. DB integration is not guaranteed.
        /// When FALSE, any command or event (sometimes queries) will be sent immediately
        /// without any wait for the transition to complete. There are multiple implementation
        /// options like outbox pattern or caching and message completion table.
        /// </summary>
        public bool Transactional { get; set; }

        /// <summary>
        /// Prefer to run a query or a command in the same process instead of scheduling a message.
        /// Queries of local services are invoked in place by default. If a command has the Persistent
        /// option, then invocation in place is only possible when the communication method supports a
        /// message lock. When message is created, you can think of this behavior as 'high priority'
        /// (cut in front of other messages on the queue) and 'low latency' (no need to wait till the
        /// message is picked up, processed, and the result is polled back.)
        /// </summary>
        public bool RunInPlace { get; set; }

        /// <summary>
        /// Ignore the transaction even if the method that calls this command (sometimes a query)
        /// is transctional. This allows better latency but no consistency.
        /// Queries ignore transactions by default.
        /// </summary>
        public bool IgnoreTransaction { get; set; }
    }
}
