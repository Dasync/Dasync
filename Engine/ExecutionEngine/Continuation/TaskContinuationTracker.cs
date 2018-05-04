using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Accessors;

namespace Dasync.ExecutionEngine.Continuation
{
    public delegate void OnTaskContinuationSetCallback(Task task, object continuation, object userData);

    public interface ITaskContinuationTracker
    {
        bool StartTracking(Task task, OnTaskContinuationSetCallback callback, object userData);
        bool CancelTracking(Task task);
    }

    public class TaskContinuationTracker : ITaskContinuationTracker
    {
        private struct TrackedTask
        {
            public WeakReference<Task> TaskReference;
            public OnTaskContinuationSetCallback Callback;
            public object UserData;
            public object Continuation;
        }

        private List<TrackedTask> _trackedTasks = new List<TrackedTask>();

        public TaskContinuationTracker()
        {
            TrackInBackground();
        }

        public bool StartTracking(Task task, OnTaskContinuationSetCallback callback, object userData)
        {
            var trackedTask = new TrackedTask
            {
                TaskReference = new WeakReference<Task>(task, trackResurrection: false),
                Callback = callback,
                UserData = userData
            };

            lock (_trackedTasks)
            {
                _trackedTasks.Add(trackedTask);
            }

            return true;
        }

        public bool CancelTracking(Task task)
        {
            throw new NotImplementedException();
        }

        private async void TrackInBackground()
        {
#warning Not ideal, but works for POC. Will optimize later.

            List<TrackedTask> tasksWithContinuation = null;

            while (true)
            {
                int delayTime;

                lock (_trackedTasks)
                {
                    if (_trackedTasks.Count == 0)
                    {
                        delayTime = 50;
                    }
                    else
                    {
                        delayTime = 5;

                        for (var i = _trackedTasks.Count - 1; i >= 0; i--)
                        {
                            var trackedTask = _trackedTasks[i];

                            if (!trackedTask.TaskReference.TryGetTarget(out var task))
                            {
                                _trackedTasks.RemoveAt(i);
                                continue;
                            }

                            trackedTask.Continuation = task.GetContinuationObject();
                            if (trackedTask.Continuation != null)
                            {
                                _trackedTasks.RemoveAt(i);
                                if (tasksWithContinuation == null)
                                    tasksWithContinuation = new List<TrackedTask>();
                                tasksWithContinuation.Add(trackedTask);
                            }
                        }
                    }
                }

                if (tasksWithContinuation != null)
                {
                    foreach (var trackedTask in tasksWithContinuation)
                    {
                        try
                        {
                            if (trackedTask.TaskReference.TryGetTarget(out var task))
                            {
                                trackedTask.TaskReference.SetTarget(null);
                                trackedTask.Callback(task, trackedTask.Continuation, trackedTask.UserData);
                            }
                        }
                        catch
                        {
                        }
                    }
                    tasksWithContinuation = null;
                }

                await Task.Delay(delayTime);
            }
        }
    }
}
