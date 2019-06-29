using Dasync.EETypes.Descriptors;
using Newtonsoft.Json;

namespace Dasync.Json
{
    /// <summary>
    /// Used for both Execute and Continue routine events.
    /// </summary>
    public class CommandEnvelope
    {
        /// <summary>
        /// Routine input parameters [execute routine].
        /// </summary>
        [JsonConverter(typeof(NestedJsonConverter))]
        public string Parameters { get; set; }

        /// <summary>
        /// Continuation of sub-routine [execute only].
        /// </summary>
        public ContinuationDescriptor Continuation { get; set; }
    }
}
