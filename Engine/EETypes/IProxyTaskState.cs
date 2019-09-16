using System.Threading.Tasks;

namespace Dasync.EETypes
{
    /// <summary>
    /// An instance of a concrete type is stored in the <see cref="Task.AsyncState"/> to denote a remote execution.
    /// </summary>
    public interface IProxyTaskState
    {
        /// <summary>
        /// Used to associate the result of a routine or a trigger with the serialized Task in a routine.
        /// </summary>
        string TaskId { get; }
    }
}
