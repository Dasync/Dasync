using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;

namespace Dasync.ExecutionEngine.Extensions
{
    public class TaskResultConverter : ITaskResultConverter
    {
        public TaskResult Convert(Task task) => task.ToTaskResult();
    }
}
