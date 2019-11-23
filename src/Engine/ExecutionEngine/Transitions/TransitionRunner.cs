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
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Eventing;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Persistence;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Resolvers;
using Dasync.EETypes.Triggers;
using Dasync.ExecutionEngine.Continuation;
using Dasync.ExecutionEngine.Extensions;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.Transitions
{
    public partial class TransitionRunner : ITransitionRunner, ILocalMethodRunner
    {
        private readonly ITransitionScope _transitionScope;
        private readonly IAsyncStateMachineMetadataProvider _asyncStateMachineMetadataProvider;
        //private readonly IServiceStateValueContainerProvider _serviceStateValueContainerProvider;
        private readonly IUniqueIdGenerator _idGenerator;
        private readonly ITaskCompletionSourceRegistry _taskCompletionSourceRegistry;
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;
        private readonly IEventResolver _eventResolver;
        private readonly ICommunicatorProvider _communicatorProvider;
        private readonly IEventPublisherProvider _eventPublisherProvider;
        private readonly IRoutineCompletionSink _routineCompletionSink;
        private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
        private readonly ISerializer _defaultSerializer;
        private readonly ISerializerProvider _serializeProvder;
        private readonly IMethodStateStorageProvider _methodStateStorageProvider;
        private readonly IValueContainerCopier _valueContainerCopier;
        private readonly IEventSubscriber _eventSubscriber;
        private readonly ITaskContinuationClassifier _taskContinuationClassifier;

        public TransitionRunner(
            ITransitionScope transitionScope,
            IAsyncStateMachineMetadataProvider asyncStateMachineMetadataProvider,
            //IServiceStateValueContainerProvider serviceStateValueContainerProvider,
            IUniqueIdGenerator idGenerator,
            ITaskCompletionSourceRegistry taskCompletionSourceRegistry,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver,
            IEventResolver eventResolver,
            ICommunicatorProvider communicatorProvider,
            IEventPublisherProvider eventPublisherProvider,
            IRoutineCompletionSink routineCompletionSink,
            ICommunicationSettingsProvider communicationSettingsProvider,
            IDefaultSerializerProvider defaultSerializerProvider,
            ISerializerProvider serializeProvder,
            IMethodStateStorageProvider methodStateStorageProvider,
            IValueContainerCopier valueContainerCopier,
            IEventSubscriber eventSubscriber,
            ITaskContinuationClassifier taskContinuationClassifier)
        {
            _transitionScope = transitionScope;
            _asyncStateMachineMetadataProvider = asyncStateMachineMetadataProvider;
            //_serviceStateValueContainerProvider = serviceStateValueContainerProvider;
            _idGenerator = idGenerator;
            _taskCompletionSourceRegistry = taskCompletionSourceRegistry;
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;
            _eventResolver = eventResolver;
            _communicatorProvider = communicatorProvider;
            _eventPublisherProvider = eventPublisherProvider;
            _routineCompletionSink = routineCompletionSink;
            _communicationSettingsProvider = communicationSettingsProvider;
            _defaultSerializer = defaultSerializerProvider.DefaultSerializer;
            _serializeProvder = serializeProvder;
            _methodStateStorageProvider = methodStateStorageProvider;
            _valueContainerCopier = valueContainerCopier;
            _eventSubscriber = eventSubscriber;
            _taskContinuationClassifier = taskContinuationClassifier;
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

        private async Task<InvokeRoutineResult> RunRoutineAsync(
            ITransitionCarrier transitionCarrier,
            TransitionDescriptor transitionDescriptor,
            CancellationToken ct)
        {
            var invocationResult = new InvokeRoutineResult();

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

                Type taskResultType =
                    methodReference.Definition.MethodInfo.ReturnType == typeof(void)
                    ? TaskAccessor.VoidTaskResultType
                    : TaskAccessor.GetTaskResultType(methodReference.Definition.MethodInfo.ReturnType);

                Task completionTask;
                IValueContainer asmValueContainer = null;

                if (TryCreateAsyncStateMachine(methodReference.Definition.MethodInfo, methodId.IntentId, out var asmInstance, out var asmMetadata))
                {
                    var isContinuation = transitionDescriptor.Type == TransitionType.ContinueRoutine;
                    asmValueContainer = await LoadRoutineStateAsync(transitionCarrier, asmInstance, asmMetadata, isContinuation, ct);

                    asmMetadata.Owner.FieldInfo?.SetValue(asmInstance, serviceInstance);

                    transitionMonitor.OnRoutineStart(
                        serviceReference,
                        methodReference,
                        methodId,
                        serviceInstance,
                        asmInstance,
                        (transitionCarrier as TransitionCarrier)?.Caller);

                    try
                    {
                        asmInstance.MoveNext();
                        completionTask = GetCompletionTask(asmInstance, asmMetadata);
                    }
                    catch (Exception ex)
                    {
                        // The MoveNext() must not throw, but instead complete the task with an error.
                        // try-catch is added just in case for a non-compiler-generated state machine.
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
                        serviceReference,
                        methodReference,
                        methodId,
                        serviceInstance,
                        routineStateMachine: null,
                        (transitionCarrier as TransitionCarrier)?.Caller);

                    try
                    {
                        completionTask = methodReference.Invoke(serviceInstance, parameters);
                    }
                    catch (Exception ex)
                    {
                        // NOTE: IMethodInvoker always throws TargetInvocationException due to the implementation details.
                        if (ex is TargetInvocationException)
                            ex = ex.InnerException;
                        completionTask = TaskAccessor.FromException(taskResultType, ex);
                    }
                }

                // NOTE: (A) method can return VOID in special cases (the Dispose method),
                // or (B) the method is not async and return NULL by mistake (assume success).
                if (completionTask == null)
                    completionTask = TaskAccessor.FromResult(taskResultType, null);

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

                    invocationResult.Outcome = InvocationOutcome.Complete;
                    invocationResult.Result = routineResult;
                }
                else
                {
                    invocationResult.Outcome = InvocationOutcome.Paused;
                }

                ScanForExtraIntents(scheduledActions);

                var commitOptions = new TransitionCommitOptions();

                await CommitAsync(scheduledActions, transitionCarrier, commitOptions, ct);
            }

            return invocationResult;
        }

        private bool TryCreateAsyncStateMachine(
            MethodInfo methodInfo,
            string intentId,
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

            //if (!string.IsNullOrEmpty(intentId) && AsyncDebugging.IsEnabled)
            //{
            //    Task debuggerTask;
            //    lock (AsyncDebugging.ActiveTasksLock)
            //    {
            //        debuggerTask = AsyncDebugging.CurrentActiveTasks.Values.FirstOrDefault(
            //            t => t.AsyncState is IProxyTaskState state && state.TaskId == intentId);
            //    }

            //    if (debuggerTask != null)
            //    {
            //        var taskBuilder = metadata.Builder.FieldInfo.GetValue(asyncStateMachine);
            //        var taskField =
            //            taskBuilder.GetType().GetField("_task", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            //            taskBuilder.GetType().GetField("m_task", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            //        if (taskField == null)
            //        {
            //            // AsyncVoidTaskBuilder
            //            var takBuilderField = taskBuilder.GetType().GetField("_builder", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            //                taskBuilder.GetType().GetField("m_builder", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            //            var subTaskBuilder = takBuilderField.GetValue(taskBuilder);
            //            var subTaskField =
            //                subTaskBuilder.GetType().GetField("_task", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            //                subTaskBuilder.GetType().GetField("m_task", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            //            subTaskField.SetValue(subTaskBuilder, debuggerTask);
            //            takBuilderField.SetValue(taskBuilder, subTaskBuilder);
            //        }
            //        else
            //        {
            //            taskField.SetValue(taskBuilder, debuggerTask);
            //        }
            //        metadata.Builder.FieldInfo.SetValue(asyncStateMachine, taskBuilder);

            //        var continuationObject = debuggerTask.GetContinuationObject();
            //        // TODO: this resets out-of-context awaiter
            //        debuggerTask.SetContinuationObject(null);
            //    }
            //}

            return true;
        }

        private Task GetCompletionTask(IAsyncStateMachine asyncStateMachine, AsyncStateMachineMetadata metadata)
        {
            // For some reason, a completion Task retrieved right after the creation of an ASM can be a different
            // than the one you get after first ASM transition. Debugger showed that the Task may get overwritten
            // in the AsyncTaskMethodBuilder.

            var taskBuilder = metadata.Builder.FieldInfo.GetValue(asyncStateMachine);
            var taskField = metadata.Builder.FieldInfo.FieldType.GetProperty("Task", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (taskField == null)
            {
                // AsyncVoidTaskBuilder
                var takBuilderField = taskBuilder.GetType().GetField("_builder", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                taskBuilder = takBuilderField.GetValue(taskBuilder);
                taskField = taskBuilder.GetType().GetProperty("Task", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
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
                if (!string.IsNullOrEmpty(transitionCarrier.ResultTaskId))
                    TaskCapture.StartCapturing();

                await transitionCarrier.ReadRoutineStateAsync(asmValueContainer, ct);

                // TODO: instead of capturing created tasks, it's easier just to go through all awaiters in the state machine. However it doe not work if a serialized Task is a variable but not inside an awaiter.
                if (!string.IsNullOrEmpty(transitionCarrier.ResultTaskId))
                {
                    UpdateTasksWithAwaitedRoutineResult(TaskCapture.FinishCapturing(), transitionCarrier);
                    UpdateTasksForDebuggerIfEnabled(asyncStateMachine, metadata, transitionCarrier);
                }
            }
            else
            {
                await transitionCarrier.ReadRoutineParametersAsync(asmValueContainer, ct);
            }

            return asmValueContainer;
        }

        private static void UpdateTasksWithAwaitedRoutineResult(
            List<Task> deserializedTasks, ITransitionCarrier carrier)
        {
            foreach (var task in deserializedTasks)
            {
                if (task.AsyncState is IProxyTaskState state &&
                    state.TaskId == carrier.ResultTaskId)
                {
                    // TODO: helper method
                    var taskResultType = TaskAccessor.GetResultType(task);
                    var expectedResultValueType = taskResultType == TaskAccessor.VoidTaskResultType ? typeof(object) : taskResultType;

                    var taskResult = carrier.ReadResult(expectedResultValueType);
                    task.TrySetResult(taskResult);
                }
            }
        }

        private void UpdateTasksForDebuggerIfEnabled(
            IAsyncStateMachine stateMachine,
            AsyncStateMachineMetadata metadata,
            ITransitionCarrier transitionCarrier)
        {
            if (!AsyncDebugging.IsEnabled)
                return;

            var resultTaskId = transitionCarrier.ResultTaskId;

            Task debuggerTask;
            lock (AsyncDebugging.ActiveTasksLock)
            {
                debuggerTask = AsyncDebugging.CurrentActiveTasks.Values.FirstOrDefault(
                    t => t.AsyncState is IProxyTaskState state && state.TaskId == resultTaskId);
            }

            if (debuggerTask == null)
                return;

            var continuationObject = debuggerTask.GetContinuationObject();
            if (continuationObject == null)
                return;

            var continuationInfo = _taskContinuationClassifier.GetContinuationInfo(continuationObject);
            if (continuationInfo.Type != TaskContinuationType.AsyncStateMachine)
                return;

            var originalStateMachine = continuationInfo.Target;

            // A state machine is uniquely identified by the Task inside the Builder.
            var originalBuilder = metadata.Builder.FieldInfo.GetValue(originalStateMachine);
            metadata.Builder.FieldInfo.SetValue(stateMachine, originalBuilder);

            var builderCompletionTask = GetCompletionTask(stateMachine, metadata);
            builderCompletionTask.SetContinuationObject(null);

            var isAwaiterFound = false;
            foreach (var localVar in metadata.LocalVariables)
            {
                if (!TaskAwaiterUtils.IsAwaiterType(localVar.FieldInfo.FieldType))
                    continue;

                var awaiter = localVar.FieldInfo.GetValue(stateMachine);
                if (awaiter == null)
                    continue;

                var task = TaskAwaiterUtils.GetTask(awaiter);
                if (task == null)
                    continue;

                if (task.AsyncState is IProxyTaskState s && s.TaskId == resultTaskId)
                {
                    isAwaiterFound = true;
                    TaskAwaiterUtils.SetTask(awaiter, debuggerTask);
                    localVar.FieldInfo.SetValue(stateMachine, awaiter);
                    break;
                }
            }

            if (!isAwaiterFound)
                return;

            // TODO: helper method
            var taskResultType = TaskAccessor.GetResultType(debuggerTask);
            var expectedResultValueType = taskResultType == TaskAccessor.VoidTaskResultType ? typeof(object) : taskResultType;
            var taskResult = transitionCarrier.ReadResult(taskResultType);

            // Reset continuation - the engine invokes MoveNext.
            debuggerTask.SetContinuationObject(null);
            debuggerTask.TrySetResult(taskResult);
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
            ITaskResult taskResult,
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
