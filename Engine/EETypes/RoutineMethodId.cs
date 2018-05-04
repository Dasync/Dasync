namespace Dasync.EETypes
{
    /// <summary>
    /// Uniquely identifies a routine method.
    /// </summary>
    public sealed class RoutineMethodId
    {
        public string MethodName { get; set; }

#warning Generic parameters
#warning Method signature hash?
#warning Does it matter if it's an interface method or not?
#warning Method/client version?
    }
}
