using System.Threading.Tasks;

namespace Dasync.EETypes.Proxy
{
    /// <summary>
    /// Used in <see cref="Task.AsyncState"/> to correlate to a routine.
    /// </summary>
    public class RoutineReference : IProxyTaskState
    {
        public string IntentId { get; set; }

        string IProxyTaskState.TaskId => IntentId;
    }
}
