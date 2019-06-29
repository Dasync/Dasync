using Dasync.ValueContainer;

namespace Dasync.EETypes.Intents
{
    public class RaiseEventIntent
    {
        /// <summary>
        /// An intent ID, which must be unique within a scope of a routine being executed.
        /// </summary>
        public string Id;

        /// <summary>
        /// A service that published the event, or null if the event is not raised by a service.
        /// </summary>
        public ServiceId ServiceId;

        public EventId EventId;

        /// <summary>
        /// Event arguments.
        /// </summary>
        public IValueContainer Parameters;
    }
}
