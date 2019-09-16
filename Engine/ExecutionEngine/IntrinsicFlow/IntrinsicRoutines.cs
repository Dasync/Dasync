using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.AsyncStateMachine;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.ExecutionEngine.Transitions;

namespace Dasync.ExecutionEngine.IntrinsicFlow
{
    public class IntrinsicRoutines
    {
        private readonly ITransitionScope _transitionScope;

        public IntrinsicRoutines(ITransitionScope transitionScope)
        {
            _transitionScope = transitionScope;
        }

        public static readonly MethodInfo WhenAllMethodInfo
            = typeof(IntrinsicRoutines).GetMethod(nameof(WhenAll));

        [AsyncStateMachine(typeof(WhenAllRoutine))]
        public Task WhenAll(Task[] tasks, ExecuteRoutineIntent[] intents, Type itemType)
        {
            var builder = AsyncTaskMethodBuilder<object>.Create();
            return builder.Start(
                new WhenAllRoutine
                {
                    __this = this,
                    __builder = builder,
                    tasks = tasks,
                    intents = intents,
                    itemType = itemType
                });
        }

        private class WhenAllRoutine : IAsyncStateMachine
        {
            internal IntrinsicRoutines __this;
            internal AsyncTaskMethodBuilder<object> __builder;
            internal Task[] tasks;
            internal ExecuteRoutineIntent[] intents;
            internal Type itemType;

            public void MoveNext()
            {
                if (intents != null)
                {
                    ExecuteRoutines();
                }
                else if (!AllTasksComplete())
                {
                    __this._transitionScope.CurrentMonitor.SaveStateWithoutResume();
                }
                else
                {
                    var result = ComposeResult();
                    CompleteRoutine(result);
                }
            }

            private void ExecuteRoutines()
            {
                var monitor = __this._transitionScope.CurrentMonitor;
                var context = monitor.Context;

                var continuation = new ContinuationDescriptor
                {
                    ServiceId = context.ServiceId,
                    Routine = context.RoutineDescriptor,
                    TaskId = context.RoutineDescriptor.IntentId
                };

                var actions = context.ScheduledActions;
                if (actions.ExecuteRoutineIntents == null)
                    actions.ExecuteRoutineIntents = new List<ExecuteRoutineIntent>(intents.Length);

                foreach (var intent in intents)
                {
                    intent.Continuation = continuation;
                    actions.ExecuteRoutineIntents.Add(intent);
                }

                intents = null;
                monitor.SaveStateWithoutResume();
            }

            private bool AllTasksComplete()
            {
                foreach (var task in tasks)
                {
                    if (!task.IsCompleted)
                    {
                        return false;
                    }
                }
                return true;
            }

            private TaskResult ComposeResult()
            {
                Array resultsArray = null;
                if (itemType != null)
                    resultsArray = Array.CreateInstance(itemType, tasks.Length);

                List<Exception> exceptions = null;

                for (var i = 0; i < tasks.Length; i++)
                {
                    var task = tasks[i];

                    if (task.IsFaulted)
                    {
                        if (exceptions == null)
                            exceptions = new List<Exception>(tasks.Length);
                        exceptions.Add(task.Exception);
                    }
                    else if (task.IsCanceled)
                    {
                        if (exceptions == null)
                            exceptions = new List<Exception>(tasks.Length);
                        exceptions.Add(new OperationCanceledException());
                    }
                    else if (resultsArray != null)
                    {
                        var taskResult = task.GetResult();
                        resultsArray.SetValue(taskResult, i);
                    }
                }

                if (exceptions?.Count > 0)
                {
                    return new TaskResult
                    {
                        Exception = new AggregateException(exceptions)
                    };
                }
                else
                {
                    return new TaskResult
                    {
                        Value = resultsArray
                    };
                }
            }

            private void CompleteRoutine(TaskResult result)
            {
                if (result.IsCanceled)
                {
                    __builder.SetResult(new OperationCanceledException());
                }
                else if (result.IsFaulted)
                {
                    __builder.SetException(result.Exception);
                }
                else
                {
                    __builder.SetResult(result.Value);
                }
            }

            void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) { }
        }
    }
}
