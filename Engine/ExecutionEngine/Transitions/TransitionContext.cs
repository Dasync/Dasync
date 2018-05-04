using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;

namespace Dasync.ExecutionEngine.Transitions
{
    public class TransitionContext
    {
        public TransitionDescriptor TransitionDescriptor;
        public ServiceId ServiceId;
        public RoutineDescriptor RoutineDescriptor;
        public object ServiceInstance;
        public MethodInfo RoutineMethod;
        public IAsyncStateMachine RoutineStateMachine;
        public Task RoutineResultTask;

        public ScheduledActions ScheduledActions = new ScheduledActions();

        // Needed for WhenAll only
        //public int WaitCount;

        public Task<ScheduledActions> TransitionCompleteTask => _transitionTcs.Task;

        public void CompleteTransition() => _transitionTcs.SetResult(ScheduledActions);

        private readonly TaskCompletionSource<ScheduledActions> _transitionTcs =
            new TaskCompletionSource<ScheduledActions>();
    }
}
