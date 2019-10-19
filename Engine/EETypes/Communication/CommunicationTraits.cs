using System;

namespace Dasync.EETypes.Communication
{
    [Flags]
    public enum CommunicationTraits
    {
        /// <summary>
        /// Messages can be lost because it is not persisted (like HTTP or RPC; can be re-tried though).
        /// </summary>
        Volatile = 1,

        MessageDeduplication = 2,

        /// <summary>
        /// Ability to lock a message on the publisher side before any consumer can read it.
        /// </summary>
        MessageLockOnPublish = 4,

        /// <summary>
        /// Ability to deliver a message at desired time.
        /// </summary>
        ScheduledDelivery = 8,

        /// <summary>
        /// Supports synchronous replies when <see cref="InvocationPreferences.Synchronous"/> is set.
        /// </summary>
        SyncReplies = 16,
    }
}
