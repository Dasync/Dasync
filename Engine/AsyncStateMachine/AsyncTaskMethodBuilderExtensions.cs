using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Dasync.AsyncStateMachine
{
    public static class AsyncTaskMethodBuilderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Start<TStateMachine>(
        this AsyncTaskMethodBuilder builder,
        TStateMachine stateMachine)
        where TStateMachine : class, IAsyncStateMachine
        {
            builder.Start(ref stateMachine);
            return builder.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Start<TStateMachine, TResult>(
            this AsyncTaskMethodBuilder<TResult> builder,
            TStateMachine stateMachine)
            where TStateMachine : class, IAsyncStateMachine
        {
            builder.Start(ref stateMachine);
            return builder.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AwaitOnCompleted<TAwaiter, TStateMachine>(
            this AsyncTaskMethodBuilder builder,
            ref TAwaiter awaiter,
            TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : class, IAsyncStateMachine
        {
            builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            this AsyncTaskMethodBuilder builder,
            ref TAwaiter awaiter,
            TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : class, IAsyncStateMachine
        {
            builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }
}
