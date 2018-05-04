using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.EETypes.Descriptors
{
    public sealed class TaskResult
    {
        /// <summary>
        /// The value of the result if the <see cref="IsSucceeded"/>.
        /// Always NULL for a <see cref="Task"/>.
        /// </summary>
        public object Value;

        /// <summary>
        /// An exception object if <see cref="IsFaulted"/>.
        /// </summary>
        public Exception Exception;

        /// <summary>
        /// When TRUE, the routine has been canceled by a <see cref="CancellationToken"/>
        /// that is associated with it. This flag is mutually exclusive with
        /// <see cref="IsSucceeded"/> and <see cref="IsFaulted"/>.
        /// </summary>
        public bool IsCanceled;

        /// <summary>
        /// When TRUE, the routine has failed with an error described in the
        /// <see cref="Exception"/> property. This flag is mutually exclusive
        /// with <see cref="IsCanceled"/> and <see cref="IsSucceeded"/>.
        /// </summary>
        public bool IsFaulted => Exception != null;

        /// <summary>
        /// When TRUE, the routine execution has succeeded, and optionally
        /// has a <see cref="Value"/> if the routine method returns generic
        /// <see cref="Task{TResult}"/>. This flag is mutually exclusive with
        /// <see cref="IsFaulted"/> and <see cref="IsCanceled"/>.
        /// </summary>
        public bool IsSucceeded => !IsCanceled && !IsFaulted;
    }
}
