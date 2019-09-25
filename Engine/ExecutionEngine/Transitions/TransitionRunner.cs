using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.AsyncStateMachine;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Resolvers;
using Dasync.EETypes.Triggers;
using Dasync.ExecutionEngine.Extensions;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.Transitions
{
    public class TransitionRunner : ITransitionRunner
    {
        private readonly ITransitionScope _transitionScope;
        private readonly ITransitionCommitter _transitionCommitter;
        private readonly IAsyncStateMachineMetadataProvider _asyncStateMachineMetadataProvider;
        //private readonly IServiceStateValueContainerProvider _serviceStateValueContainerProvider;
        private readonly IUniqueIdGenerator _idGenerator;
        private readonly ITaskCompletionSourceRegistry _taskCompletionSourceRegistry;
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;

        public TransitionRunner(
            ITransitionScope transitionScope,
            ITransitionCommitter transitionCommitter,
            IAsyncStateMachineMetadataProvider asyncStateMachineMetadataProvider,
            //IServiceStateValueContainerProvider serviceStateValueContainerProvider,
            IUniqueIdGenerator idGenerator,
            ITaskCompletionSourceRegistry taskCompletionSourceRegistry,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver)
        {
            _transitionScope = transitionScope;
            _transitionCommitter = transitionCommitter;
            _asyncStateMachineMetadataProvider = asyncStateMachineMetadataProvider;
            //_serviceStateValueContainerProvider = serviceStateValueContainerProvider;
            _idGenerator = idGenerator;
            _taskCompletionSourceRegistry = taskCompletionSourceRegistry;
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;
        }

        public async Task RunAsync(
            ITransitionCarrier transitionCarrier,
            CancellationToken ct)
        {
            var transitionDescriptor = await transitionCarrier.GetTransitionDescriptorAsync(ct);

            if (transitionDescriptor.Type == TransitionType.InvokeRoutine ||
                transitionDescriptor.Type == TransitionType.ContinueRoutine)
            {
                await RunRoutineAsync(transitionCarrier, transitionDescriptor, ct);
            }
            else
            {
                throw new InvalidOperationException($"Unknown transition type '{transitionDescriptor.Type}'.");
            }
        }

        private async Task RunRoutineAsync(
            ITransitionCarrier transitionCarrier,
            TransitionDescriptor transitionDescriptor,
            CancellationToken ct)
        {
            using (_transitionScope.Enter(transitionDescriptor))
            {
                var transitionMonitor = _transitionScope.CurrentMonitor;

                var serviceId = await transitionCarrier.GetServiceIdAsync(ct);
                var methodId = await transitionCarrier.GetRoutineDescriptorAsync(ct);

                var serviceReference = _serviceResolver.Resolve(serviceId);
                var methodReference = _methodResolver.Resolve(serviceReference.Definition, methodId);

                object serviceInstance = serviceReference.GetInstance();

                //var serviceStateContainer = _serviceStateValueContainerProvider.CreateContainer(serviceInstance);
                //var isStatefullService = serviceStateContainer.GetCount() > 0;
                //if (isStatefullService)
                //    await transitionCarrier.ReadServiceStateAsync(serviceStateContainer, ct);

                Task completionTask;
                IValueContainer asmValueContainer = null;

                if (TryCreateAsyncStateMachine(methodReference.Definition.MethodInfo, out var asmInstance, out var asmMetadata))
                {
                    var isContinuation = transitionDescriptor.Type == TransitionType.ContinueRoutine;
                    asmValueContainer = await LoadRoutineStateAsync(transitionCarrier, asmInstance, asmMetadata, isContinuation, ct);

                    asmMetadata.Owner.FieldInfo?.SetValue(asmInstance, serviceInstance);

                    transitionMonitor.OnRoutineStart(
                        serviceId,
                        methodId,
                        serviceInstance,
                        methodReference.Definition.MethodInfo,
                        asmInstance);

                    try
                    {
#warning possibly need to create a proxy? on a sealed ASM class? How to capture Task.Delay if it's not immediate after first MoveNext?
                        asmInstance.MoveNext();
                        completionTask = GetCompletionTask(asmInstance, asmMetadata);
                    }
                    catch (Exception ex)
                    {
                        // The MoveNext() must not throw, but instead complete the task with an error.
                        // try-catch is added just in case for a non-compiler-generated state machine.
                        var taskResultType = TaskAccessor.GetTaskResultType(methodReference.Definition.MethodInfo.ReturnType);
                        completionTask = TaskAccessor.FromException(taskResultType, ex);
                    }
                }
                else
                {
                    if (transitionDescriptor.Type == TransitionType.ContinueRoutine)
                        throw new InvalidOperationException("Cannot continue a routine because it's not a state machine.");

                    var parameters = methodReference.CreateParametersContainer();
                    await transitionCarrier.ReadRoutineParametersAsync(parameters, ct);

                    transitionMonitor.OnRoutineStart(
                        serviceId,
                        methodId,
                        serviceInstance,
                        methodReference.Definition.MethodInfo,
                        routineStateMachine: null);

                    try
                    {
                        completionTask = methodReference.Invoke(serviceInstance, parameters);
                    }
                    catch (Exception ex)
                    {
#warning IDisposable.Dispose returns void, not a Task
                        var taskResultType = TaskAccessor.GetTaskResultType(methodReference.Definition.MethodInfo.ReturnType);
                        completionTask = TaskAccessor.FromException(taskResultType, ex);
                    }
                }

                if (completionTask == null)
                {
#warning Check if this method is really IDiposable.Dispose() ?
                    if (methodReference.Definition.MethodInfo.Name == "Dispose")
                    {
                        completionTask = TaskAccessor.CompletedTask;
                    }
                    else
                    {
                        // This is possible if the routine is not marked as 'async' and just returns a NULL result.
                        throw new Exception("Critical: a routine method returned null task");
                    }
                }

                var scheduledActions = await transitionMonitor.TrackRoutineCompletion(completionTask);

                if (scheduledActions.SaveRoutineState /*|| isStatefullService*/)
                {
                    scheduledActions.SaveStateIntent = new SaveStateIntent
                    {
                        Service = serviceId,
                        //ServiceState = isStatefullService ? serviceStateContainer : null,
                        Method = scheduledActions.SaveRoutineState ? methodId : null,
                        RoutineState = scheduledActions.SaveRoutineState ? asmValueContainer : null,
                        AwaitedRoutine = scheduledActions.ExecuteRoutineIntents?.FirstOrDefault(
                            intent => intent.Continuation?.Method?.IntentId == methodId.IntentId)
                    };
                }

                if (scheduledActions.ResumeRoutineIntent != null)
                {
                    scheduledActions.ResumeRoutineIntent.Id = _idGenerator.NewId();
                }

                if (completionTask.IsCompleted)
                {
                    var routineResult = completionTask.ToTaskResult();

                    scheduledActions.SaveStateIntent.RoutineResult = routineResult;

                    var taskId = methodId.IntentId;

                    await AddContinuationIntentsAsync(
                        transitionCarrier,
                        scheduledActions,
                        routineResult,
                        taskId,
                        ct);
                }

                ScanForExtraIntents(scheduledActions);

                var commitOptions = new TransitionCommitOptions();

                await _transitionCommitter.CommitAsync(scheduledActions, transitionCarrier, commitOptions, ct);
            }
        }

        private bool TryCreateAsyncStateMachine(
            MethodInfo methodInfo,
            out IAsyncStateMachine asyncStateMachine,
            out AsyncStateMachineMetadata metadata)
        {
            if (!methodInfo.IsAsyncStateMachine())
            {
                asyncStateMachine = null;
                metadata = null;
                return false;
            }

            metadata = _asyncStateMachineMetadataProvider.GetMetadata(methodInfo);
            // ASM is a struct in 'release' mode, thus need to box it.
            asyncStateMachine = (IAsyncStateMachine)Activator.CreateInstance(metadata.StateMachineType);
            metadata.State.FieldInfo?.SetValue(asyncStateMachine, -1);
            return true;
        }

        private Task GetCompletionTask(IAsyncStateMachine asyncStateMachine, AsyncStateMachineMetadata metadata)
        {
            // For some reason, a completion Task retrieved right after the creation of an ASM can be a different
            // than the one you get after first ASM transition. Debugger showed that the Task may get overwritten
            // in the AsyncTaskMethodBuilder.

            var taskBuilder = metadata.Builder.FieldInfo.GetValue(asyncStateMachine);
            var taskField = metadata.Builder.FieldInfo.FieldType.GetProperty("Task", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var completionTask = (Task)taskField.GetValue(taskBuilder); // builder is a struct, need to initialize the Task here!
            return completionTask;
        }

        private async Task<IValueContainer> LoadRoutineStateAsync(
            ITransitionCarrier transitionCarrier,
            IAsyncStateMachine asyncStateMachine,
            AsyncStateMachineMetadata metadata,
            bool isContinuation,
            CancellationToken ct)
        {
            var asmValueContainer = GetValueContainerProxy(asyncStateMachine, metadata);

            if (isContinuation)
            {
                var awaitedResult = await transitionCarrier.GetAwaitedResultAsync(ct);
                if (awaitedResult != null)
                    TaskCapture.StartCapturing();

                await transitionCarrier.ReadRoutineStateAsync(asmValueContainer, ct);

                if (awaitedResult != null)
                    UpdateTasksWithAwaitedRoutineResult(
                        TaskCapture.FinishCapturing(), awaitedResult);
            }
            else
            {
                await transitionCarrier.ReadRoutineParametersAsync(asmValueContainer, ct);
            }

            return asmValueContainer;
        }

        private static void UpdateTasksWithAwaitedRoutineResult(
            List<Task> deserializedTasks, ResultDescriptor awaitedResult)
        {
            foreach (var task in deserializedTasks)
            {
                if (task.AsyncState is IProxyTaskState state &&
                    state.TaskId == awaitedResult.TaskId)
                {
                    task.TrySetResult(awaitedResult.Result);
                }
            }
        }

        private static IValueContainer GetValueContainerProxy(
            IAsyncStateMachine asyncStateMachine,
            AsyncStateMachineMetadata metadata)
        {
            var allFields = GetFields(metadata);
            var fieldDescs = allFields.Select(arg => new KeyValuePair<string, MemberInfo>(
                string.IsNullOrEmpty(arg.Name) ? arg.InternalName : arg.Name, arg.FieldInfo));
            return ValueContainerFactory.CreateProxy(asyncStateMachine, fieldDescs);
        }

        private static IEnumerable<AsyncStateMachineField> GetFields(AsyncStateMachineMetadata metadata)
        {
            if (metadata.State.FieldInfo != null)
                yield return metadata.State;

            if (metadata.InputArguments != null)
                foreach (var field in metadata.InputArguments)
                    yield return field;

            if (metadata.LocalVariables != null)
                foreach (var field in metadata.LocalVariables)
                    yield return field;
        }

        private async Task AddContinuationIntentsAsync(
            ITransitionCarrier transitionCarrier,
            ScheduledActions actions,
            TaskResult taskResult,
            string taskId,
            CancellationToken ct)
        {
            var continuations = await transitionCarrier.GetContinuationsAsync(ct);
            if (continuations?.Count > 0)
            {
                actions.ContinuationIntents = new List<ContinueRoutineIntent>(continuations.Count);
                foreach (var continuation in continuations)
                {
                    var intent = new ContinueRoutineIntent
                    {
                        Id = _idGenerator.NewId(),
                        Service = continuation.Service,
                        Method = continuation.Method,
                        ContinueAt = continuation.ContinueAt,
                        TaskId = taskId,
                        Result = taskResult
                    };
                    actions.ContinuationIntents.Add(intent);
                }
            }
        }

#warning This method needs to be extracted into a separate class
        private void ScanForExtraIntents(ScheduledActions scheduledActions)
        {
            if (scheduledActions.ExecuteRoutineIntents?.Count > 0)
            {
                foreach (var intent in scheduledActions.ExecuteRoutineIntents)
                {
                    var parameters = intent.Parameters;
                    if (parameters != null)
                    {
                        for (var i = 0; i < parameters.GetCount(); i++)
                            AddIntentOnSpecialValue(parameters.GetValue(i));
                    }
                }
            }

            if (scheduledActions.RaiseEventIntents?.Count > 0)
            {
                foreach (var intent in scheduledActions.RaiseEventIntents)
                {
                    var parameters = intent.Parameters;
                    if (parameters != null)
                    {
                        for (var i = 0; i < parameters.GetCount(); i++)
                            AddIntentOnSpecialValue(parameters.GetValue(i));
                    }
                }
            }

            if (scheduledActions.SaveStateIntent != null)
            {
                var values = scheduledActions.SaveStateIntent.RoutineState;
                if (values != null)
                {
                    for (var i = 0; i < values.GetCount(); i++)
                        AddIntentOnSpecialValue(values.GetValue(i));
                }
                AddIntentOnSpecialValue(scheduledActions.SaveStateIntent.RoutineResult?.Value);
            }

            void AddIntentOnSpecialValue(object value)
            {
                if (value == null)
                    return;

                if (value is CancellationToken cancellationToken)
                {
                    return;
                }
                else if (TaskCompletionSourceAccessor.IsTaskCompletionSource(value))
                {
                    if (_taskCompletionSourceRegistry.TryRegisterNew(value, out var triggerReference))
                    {
                        if (scheduledActions.RegisterTriggerIntents == null)
                            scheduledActions.RegisterTriggerIntents = new List<RegisterTriggerIntent>();

                        scheduledActions.RegisterTriggerIntents.Add(
                            new RegisterTriggerIntent
                            {
                                TriggerId = triggerReference.Id,
                                ValueType = TaskCompletionSourceAccessor.GetTask(value).GetResultType()
                            });
                    }
                }
            }
        }
    }
}
