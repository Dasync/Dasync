﻿namespace Dasync.EETypes.Platform
{
    public struct TransitionCommitOptions
    {
        /// <summary>
        /// A hint to notify current process on routine completion (synchronous call).
        /// The listener must use <see cref="IRoutineCompletionNotifier.NotifyOnCompletion"/>.
        /// </summary>
        public bool NotifyOnRoutineCompletion;

        public string RequestId;

        public string CorrelationId;
    }
}
