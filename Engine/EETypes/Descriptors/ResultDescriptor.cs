namespace Dasync.EETypes.Descriptors
{
    public sealed class ResultDescriptor
    {
        /// <summary>
        /// The result of the awaited routine. 
        /// </summary>
        public TaskResult Result;

        /// <summary>
        /// The <see cref="ExecuteRoutineIntent.Id"/> for awaited routine, which will be
        /// used to correlate serialized proxy tasks with <see cref="Result"/>.
        /// </summary>
        public long CorrelationId;
    }
}
