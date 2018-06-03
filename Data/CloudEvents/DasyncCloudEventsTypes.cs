namespace Dasync.CloudEvents
{
    public class DasyncCloudEventsTypes
    {
        public struct EventType
        {
            public string Name;
            public string Version;
        }

        public static readonly EventType InvokeRoutine = new EventType
        {
            Name = "dasync.routine.invoke",
            Version = "0.1"
        };

        public static readonly EventType ContinueRoutine = new EventType
        {
            Name = "dasync.routine.continue",
            Version = "0.1"
        };
    }
}
