using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dasync.Accessors
{
    public sealed class TaskCapture
    {
        private readonly static AsyncLocal<TaskCapture> _current = new AsyncLocal<TaskCapture>();

        private List<Task> _tasks = new List<Task>();

        public static void StartCapturing()
        {
            _current.Value = new TaskCapture();
        }

        public static List<Task> FinishCapturing()
        {
            var result = _current.Value._tasks;
            _current.Value = null;
            return result;
        }

        public static void CaptureTask(Task task)
        {
            _current.Value?._tasks.Add(task);
        }
    }
}
