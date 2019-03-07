using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Platform;

namespace Dasync.AspNetCore.Platform
{
    public interface IRoutineCompletionSink
    {
        void OnRoutineCompleted(long intentId, TaskResult routineResult);
    }

    public class RoutineCompletionNotifier : IRoutineCompletionNotifier, IRoutineCompletionSink
    {
        private Dictionary<long, TaskCompletionSource<TaskResult>> _sinks = new Dictionary<long, TaskCompletionSource<TaskResult>>();

        public void NotifyCompletion(long intentId, TaskCompletionSource<TaskResult> completionSink)
        {
            lock (_sinks)
            {
#warning Multiple sinks per intent?
                _sinks.Add(intentId, completionSink);
            }
        }

        public void OnRoutineCompleted(long intentId, TaskResult routineResult)
        {
            TaskCompletionSource<TaskResult> sink;
            lock (_sinks)
            {
                if (_sinks.TryGetValue(intentId, out sink))
                    _sinks.Remove(intentId);
            }
            if (sink != null)
                sink.SetResult(routineResult);
        }
    }
}
