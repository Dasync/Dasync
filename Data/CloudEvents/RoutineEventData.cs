using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Newtonsoft.Json;

namespace Dasync.CloudEvents
{
    /// <summary>
    /// Used for both Execute and Continue routine events.
    /// </summary>
    public class RoutineEventData
    {
        public ServiceId ServiceId { get; set; }

        public RoutineDescriptor Routine { get; set; }

        /// <summary>
        /// Caller of a sub-routine [execute only].
        /// </summary>
        public CallerDescriptor Caller { get; set; }

        /// <summary>
        /// The sub-routine returning a result [continue only].
        /// </summary>
        public CallerDescriptor Callee { get; set; }

        /// <summary>
        /// Continuation of sub-routine [execute only].
        /// </summary>
        public ContinuationDescriptor Continuation { get; set; }

        /// <summary>
        /// Routine input parameters [execute routine].
        /// </summary>
        [JsonConverter(typeof(NestedJsonConverter))]
        public string Parameters { get; set; }

        /// <summary>
        /// Result of a sub-routine [continue only].
        /// </summary>
        [JsonConverter(typeof(NestedJsonConverter))]
        public string Result { get; set; }
    }
}
