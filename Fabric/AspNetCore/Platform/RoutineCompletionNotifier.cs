using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Platform;

namespace Dasync.AspNetCore.Platform
{
    public interface IRoutineCompletionSink
    {
        void OnRoutineCompleted(string intentId, TaskResult routineResult);
    }

    public class RoutineCompletionNotifier : IRoutineCompletionNotifier, IRoutineCompletionSink
    {
        private Dictionary<string, List<TaskCompletionSource<TaskResult>>> _sinks = new Dictionary<string, List<TaskCompletionSource<TaskResult>>>();
        private Dictionary<string, TaskResult> _recentResults = new Dictionary<string, TaskResult>();
        private const int MaxRecentResults = 100;

        public Task<TaskResult> TryPollCompletionAsync(
            ServiceId serviceId,
            RoutineMethodId methodId,
            string intentId,
            CancellationToken ct)
        {
            lock (_recentResults)
            {
                _recentResults.TryGetValue(intentId, out var taskResult);
                TrimStaleResults();
                return Task.FromResult(taskResult);
            }
        }

        public void NotifyCompletion(
            ServiceId serviceId,
            RoutineMethodId methodId,
            string intentId,
            TaskCompletionSource<TaskResult> completionSink,
            CancellationToken ct)
        {
            var taskResult = TryPollCompletionAsync(serviceId, methodId, intentId, default).Result;
            if (taskResult != null)
            {
                completionSink.SetResult(taskResult);
                return;
            }

            lock (_sinks)
            {
                if (!_sinks.TryGetValue(intentId, out var set))
                    _sinks.Add(intentId, set = new List<TaskCompletionSource<TaskResult>>());
                set.Add(completionSink);
            }
        }

        public void OnRoutineCompleted(string intentId, TaskResult routineResult)
        {
            lock (_recentResults)
            {
                TrimStaleResults();
                _recentResults[intentId] = routineResult;
            }

            List<TaskCompletionSource<TaskResult>> set;
            lock (_sinks)
            {
                if (_sinks.TryGetValue(intentId, out set))
                    _sinks.Remove(intentId);
            }
            if (set != null)
            {
                foreach (var sink in set)
                    sink.SetResult(routineResult);
            }
        }

        private void TrimStaleResults()
        {
            if (_recentResults.Count > MaxRecentResults)
            {
                var keys = _recentResults.Keys.Take(_recentResults.Count - MaxRecentResults).ToList();
                foreach (var key in keys)
                    _recentResults.Remove(key);
            }
        }
    }
}
