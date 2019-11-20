using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Resolvers;

namespace Dasync.ExecutionEngine.Transitions
{
    public class TransitionContext : ITransitionContext
    {
        public TransitionDescriptor TransitionDescriptor;
        //public ServiceId ServiceId;
        public PersistedMethodId MethodId;
        public object ServiceInstance;
        public MethodInfo RoutineMethod;
        public IAsyncStateMachine RoutineStateMachine;
        public Task RoutineResultTask;
        public IServiceReference ServiceRef;
        public IMethodReference MethodRef;
        public CallerDescriptor Caller;

        public ScheduledActions ScheduledActions = new ScheduledActions();

        // Needed for WhenAll only
        //public int WaitCount;

        public Task<ScheduledActions> TransitionCompleteTask => _transitionTcs.Task;

        public void CompleteTransition() => _transitionTcs.SetResult(ScheduledActions);

        private readonly TaskCompletionSource<ScheduledActions> _transitionTcs =
            new TaskCompletionSource<ScheduledActions>();

        #region ITransitionContext

        ServiceId ITransitionContext.Service => ServiceRef.Id?.Clone();

        MethodId ITransitionContext.Method => MethodId?.Clone();

        string ITransitionContext.IntentId => MethodId?.IntentId;

        CallerDescriptor ITransitionContext.Caller => Caller;

        Dictionary<string, string> ITransitionContext.FlowContext => null;

        #endregion
    }
}
