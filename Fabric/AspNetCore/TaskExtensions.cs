using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.AspNetCore
{
    public static class TaskExtensions
    {
        public static Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            if (task.IsCompleted)
                return task;

            var timeoutMilliseconds = (int)timeout.TotalMilliseconds;
            if (timeoutMilliseconds <= 1)
                return Task.FromCanceled<T>(default);

            var tcs = new TaskCompletionSource<T>();
            var timer = new Timer(CallbackHandler<T>.OnTimerTickCallback, tcs, timeoutMilliseconds, Timeout.Infinite);
            task.ContinueWith(CallbackHandler<T>.OnTaskCompleteCallback, tcs);

            return tcs.Task;
        }

        private class CallbackHandler<T>
        {
            internal static readonly TimerCallback OnTimerTickCallback = OnTimerTick;
            internal static readonly Action<Task<T>, object> OnTaskCompleteCallback = OnTaskComplete;

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
