using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Dasync.CloudEvents
{
    [StructLayout(LayoutKind.Sequential)]
    public class RoutineEventEnvelope // DO NOT DERIVE FROM CloudEventsEnvelope! Need to preserve exact property order.
    {
        #region CloudEventsEnvelope

        public string CloudEventsVersion { get; set; }

        public string EventType { get; set; }

        public string EventTypeVersion { get; set; }

        public string Source { get; set; }

        public string EventID { get; set; }

        public DateTimeOffset EventTime { get; set; }

        public string SchemaURL { get; set; }

        public string ContentType { get; set; }

        public Dictionary<string, dynamic> Extensions { get; set; }

        #endregion

        #region RoutineEventEnvelope

        public string ETag { get; set; }

        public DateTimeOffset? EventDeliveryTime { get; set; }

        #endregion

        #region CloudEventsEnvelope

        [JsonConverter(typeof(NestedJsonConverter))]
        public string Data { get; set; }

        #endregion
    }
}
