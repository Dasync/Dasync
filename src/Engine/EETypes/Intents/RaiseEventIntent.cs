using System;
using System.Runtime.InteropServices;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Intents
{
    [StructLayout(LayoutKind.Sequential)]
    public class RaiseEventIntent
    {
        /// <summary>
        /// An intent ID, which must be unique within a scope of a routine being executed.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// A service that published the event, or null if the event is not raised by a service.
        /// </summary>
        public ServiceId Service { get; set; }

        public EventId Event { get; set; }

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Event arguments.
        /// </summary>
        public IValueContainer Parameters { get; set; }
    }
}
