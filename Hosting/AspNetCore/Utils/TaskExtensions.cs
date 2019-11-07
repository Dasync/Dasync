using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.Hosting.AspNetCore.Utils
{
    public static class TaskExtensions
    {
        public static Task<T> WithTimeout<T>(this Task<T> task, TimeSpan? timeout)
        {
            if (task.IsCompleted || !timeout.HasValue)
                return task;

            var timeoutMilliseconds = (int)timeout.Value.TotalMilliseconds;
            if (timeoutMilliseconds <= 1)
                return CallbackHandler<T>.CanceledTask;

            var tcs = new TaskCompletionSource<T>();
            var timer = new Timer(CallbackHandler<T>.OnTimerTickCallback, tcs, timeoutMilliseconds, Timeout.Infinite);
            task.ContinueWith(CallbackHandler<T>.OnTaskCompleteCallback, tcs, TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        public static TaskCompletionSource<T> WithTimeout<T>(this TaskCompletionSource<T> tcs, TimeSpan? timeout)
        {
            if (!timeout.HasValue)
                return tcs;

            var timeoutMilliseconds = (int)timeout.Value.TotalMilliseconds;
            if (timeoutMilliseconds <= 1)
            {
                tcs.TrySetCanceled();
                return tcs;
            }

            var timer = new Timer(CallbackHandler<T>.OnTimerTickCallback, tcs, timeoutMilliseconds, Timeout.Infinite);
            GC.KeepAlive(timer);
            return tcs;
        }

        private class CallbackHandler<T>
        {
            internal static readonly Task<T> CanceledTask;
            internal static readonly TimerCallback OnTimerTickCallback = OnTimerTick;
            internal static readonly Action<Task<T>, object> OnTaskCompleteCallback = OnTaskComplete;

            static CallbackHandler()
            {
                var tcs = new TaskCompletionSource<T>();
                tcs.SetCanceled();
                CanceledTask = tcs.Task;
            }

            private static void OnTimerTick(object state)
            {
                var tcs = (TaskCompletionSource<T>)state;
                tcs.TrySetCanceled();
            }

            private static void OnTaskComplete(Task<T> task, object state)
            {
                var tcs = (TaskCompletionSource<T>)state;
                if (task.IsCanceled)
                    tcs.TrySetCanceled();
                else if (task.IsFaulted)
                    tcs.TrySetException(task.Exception);
                else
                    tcs.TrySetResult(task.Result);
            }
        }
    }
}
