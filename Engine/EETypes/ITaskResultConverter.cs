using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes
{
    public interface ITaskResultConverter
    {
        TaskResult Convert(Task task);
    }
}
