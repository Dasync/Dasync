using System;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Communication
{
    // TODO: do not derive from IInvocationData - no flow context
    public interface IMethodContinuationData : IInvocationData
    {
        ServiceId Service { get; }

        PersistedMethodId Method { get; }

        /// <summary>
        /// The <see cref="ExecuteRoutineIntent.Id"/> for awaited routine, which will be
        /// used to correlate serialized proxy tasks with <see cref="ContinueRoutineIntent.Result"/>.
        /// </summary>
        string TaskId { get; }

        /// <summary>
        /// The result of the awaited routine. 
        /// </summary>
        TaskResult ReadResult(Type expectedResultValueType);
    }
}
