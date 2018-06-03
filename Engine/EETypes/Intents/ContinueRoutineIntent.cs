using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Intents
{
    public sealed class ContinueRoutineIntent
    {
        public long Id;

        public ContinuationDescriptor Continuation;

        public RoutineResultDescriptor Result;

        /// <summary>
        /// Describes the routine of a service that was called and is returning
        /// the call back with the produced <see cref="Result"/>.
        /// </summary>
        public CallerDescriptor Callee;
    }
}
