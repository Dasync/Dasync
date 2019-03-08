using Newtonsoft.Json;

namespace Dasync.Json
{
    /// <summary>
    /// Used for both Execute and Continue routine events.
    /// </summary>
    public class EventEnvelope
    {
        /// <summary>
        /// Routine input parameters [execute routine].
        /// </summary>
        [JsonConverter(typeof(NestedJsonConverter))]
        public string Parameters { get; set; }
    }
}
