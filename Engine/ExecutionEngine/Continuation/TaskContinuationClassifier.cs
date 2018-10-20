using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Accessors;

namespace Dasync.ExecutionEngine.Continuation
{
    public interface ITaskContinuationClassifier
    {
        TaskContinuationInfo GetContinuationInfo(object continuationObject);
    }

    public class TaskContinuationClassifier : ITaskContinuationClassifier
    {
        public TaskContinuationInfo GetContinuationInfo(object continuationObject)
        {
            ExecutionContext capturedContext = null;

            while (true)
            {
                if (continuationObject == null || ReferenceEquals(continuationObject, TaskAccessor.CompletionSentinel))
                {
                    return new TaskContinuationInfo
                    {
                        Type = TaskContinuationType.None
                    };
                }

                if (WhenAllPromiseAccessor.TryGetTasks(continuationObject, out Array tasks))
                {
                    return new TaskContinuationInfo
                    {
                        Type = TaskContinuationType.WhenAll,
                        Target = continuationObject, // devires from Task
                        Items = tasks,
                        CapturedContext = capturedContext
                    };
                }

                if (continuationObject is Task task)
                {
                    continuationObject = task.GetContinuationObject();
                    continue;
                }

                if (continuationObject is List<object> continuationList)
                {
                    if (continuationList.Count == 0)
                    {
                        return new TaskContinuationInfo
                        {
                            Type = TaskContinuationType.None
                        };
                    }
                    else if (continuationList.Count == 1)
                    {
                        continuationObject = continuationList[0];
                        continue;
                    }
                    else
                    {
                        return new TaskContinuationInfo
                        {
                            Type = TaskContinuationType.ContinuationList,
                            Target = null,
                            Items = continuationList,
                            CapturedContext = capturedContext
                        };
                    }
                }

                if (ReferenceEquals(continuationObject.GetType(), StandardTaskContinuationAccessor.StandardTaskContinuationType))
                {
                    return new TaskContinuationInfo
                    {
                        Type = TaskContinuationType.Standard,
                        Target = StandardTaskContinuationAccessor.GetTask(continuationObject),
                        Options = StandardTaskContinuationAccessor.GetOptions(continuationObject),
                        CapturedContext = capturedContext
                    };
                }

                if (AwaitTaskContinuationAccessor.IsAwaitTaskContinuation(continuationObject))
                {
                    capturedContext = AwaitTaskContinuationAccessor.GetContext(continuationObject);
                    continuationObject = AwaitTaskContinuationAccessor.GetAction(continuationObject);
                    continue;
                }

                if (SynchronizationContextAwaitTaskContinuationAccessor.TryGetAction(
                    continuationObject, out var action))
                {
                    continuationObject = action;
                    continue;
                }


                if (continuationObject is Delegate @delegate && @delegate.Target != null)
                {
                    var targetType = @delegate.Target.GetType();

                    if (ContinuationWrapperAccessor.ContinuationWrapperType.IsAssignableFrom(targetType))
                    {
                        continuationObject = ContinuationWrapperAccessor.GetContinuation(@delegate.Target);
                        continue;
                    }

                    if (AsyncStateMachineBoxAccessor.IsAsyncStateMachineBox(targetType))
                    {
                        return new TaskContinuationInfo
                        {
                            Type = TaskContinuationType.AsyncStateMachine,
                            Target = AsyncStateMachineBoxAccessor.GetStateMachine(@delegate.Target),
                            CapturedContext = AsyncStateMachineBoxAccessor.GetContext(@delegate.Target) ?? capturedContext
                        };
                    }

                    if (MoveNextRunnerAccessor.IsMoveNextRunner(targetType))
                    {
                        return new TaskContinuationInfo
                        {
                            Type = TaskContinuationType.AsyncStateMachine,
                            Target = MoveNextRunnerAccessor.GetStateMachine(@delegate.Target),
                            CapturedContext = MoveNextRunnerAccessor.GetContext(@delegate.Target) ?? capturedContext
                        };
                    }

                    if (SynchronizationContextAwaitTaskContinuationAccessor.TryGetAction(
                        @delegate.Target, out action))
                    {
                        continuationObject = action;
                        continue;
                    }
                }

                return new TaskContinuationInfo
                {
                    Type = TaskContinuationType.Unknown,
                    Target = continuationObject,
                    CapturedContext = capturedContext
                };
            }
        }
    }
}
