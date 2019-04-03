using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Intents
{
    public sealed class ContinueRoutineIntent
    {
        public string Id;

        public ContinuationDescriptor Continuation;

        public ResultDescriptor Result;

        /// <summary>
        /// Describes the routine of a service that was called and is returning
        /// the call back with the produced <see cref="Result"/>.
        /// </summary>
        public CallerDescriptor Callee;
    }
}
