using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.EETypes.Platform;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.Transitions
{
    internal class TransitionCarrier : ITransitionCarrier
    {
        private readonly MethodInvocationData _methodInvocationData;
        private readonly MethodContinuationData _methodContinuationData;
        private MethodExecutionState _methodExecutionState;
        private IValueContainerCopier _valueContainerCopier;

        public CallerDescriptor Caller { get; private set; }

        public TransitionCarrier(
            MethodInvocationData methodInvocationData,
            IValueContainerCopier valueContainerCopier,
            ICommunicatorMessage message)
        {
            _methodInvocationData = methodInvocationData;
            Caller = methodInvocationData.Caller;
            _valueContainerCopier = valueContainerCopier;
            Message = message;
        }

        public TransitionCarrier(
            MethodContinuationData routineContinuationData,
            ICommunicatorMessage message)
        {
            _methodContinuationData = routineContinuationData;
            Message = message;
        }

        public void SetMethodExecutionState(MethodExecutionState methodExecutionState, IValueContainerCopier valueContainerCopier)
        {
            _methodExecutionState = methodExecutionState;
            Caller = _methodExecutionState.Caller;
            _valueContainerCopier = valueContainerCopier;
        }

        public SerializedMethodContinuationState ContinuationState
        {
            get
            {
                if (_methodExecutionState != null)
                    return _methodExecutionState.ContinuationState;

                if (_methodContinuationData != null)
                    return _methodContinuationData.State;

                return null;
            }
        }

        public string ResultTaskId => _methodContinuationData?.TaskId;

        public ICommunicatorMessage Message { get; }

        public ITaskResult ReadResult(Type expectedResultValueType)
        {
            if (_methodContinuationData == null)
                throw new InvalidOperationException();

            var taskResult = TaskResult.CreateEmpty(expectedResultValueType);
            _valueContainerCopier.CopyValues(
                source: _methodContinuationData.Result,
                destination: (IValueContainer)taskResult);

            return taskResult;
        }

        public Task<List<ContinuationDescriptor>> GetContinuationsAsync(CancellationToken ct)
        {
            List<ContinuationDescriptor> result = null;
            if (_methodInvocationData?.Continuation != null)
            {
                result = new List<ContinuationDescriptor>();
                result.Add(_methodInvocationData.Continuation);
            }
            else if (_methodExecutionState?.Continuation != null)
            {
                result = new List<ContinuationDescriptor>();
                result.Add(_methodExecutionState.Continuation);
            }
            return Task.FromResult(result);
        }

        public Task<PersistedMethodId> GetRoutineDescriptorAsync(CancellationToken ct)
        {
            if (_methodExecutionState != null)
            {
                return Task.FromResult(_methodExecutionState.Method);
            }
            else if (_methodInvocationData != null)
            {
                var result = _methodInvocationData.Method.CopyTo(
                    new PersistedMethodId
                    {
                        IntentId = _methodInvocationData.IntentId,
                    });
                return Task.FromResult(result);
            }
            else if (_methodContinuationData != null)
            {
                return Task.FromResult(_methodContinuationData.Method);
            }
            throw new InvalidOperationException();
        }

        public Task<ServiceId> GetServiceIdAsync(CancellationToken ct)
        {
            if (_methodExecutionState != null)
            {
                return Task.FromResult(_methodExecutionState.Service);
            }
            else if (_methodInvocationData != null)
            {
                return Task.FromResult(_methodInvocationData.Service);
            }
            else if (_methodContinuationData != null)
            {
                return Task.FromResult(_methodContinuationData.Service);
            }
            throw new InvalidOperationException();
        }

        public Task<TransitionDescriptor> GetTransitionDescriptorAsync(CancellationToken ct)
        {
            if (_methodInvocationData != null)
                return Task.FromResult(new TransitionDescriptor { Type = TransitionType.InvokeRoutine });
            if (_methodContinuationData != null)
                return Task.FromResult(new TransitionDescriptor { Type = TransitionType.ContinueRoutine });
            throw new InvalidOperationException();
        }

        public Task ReadRoutineParametersAsync(IValueContainer target, CancellationToken ct)
        {
            if (_methodInvocationData != null)
            {
                _valueContainerCopier.CopyValues(
                    source: _methodInvocationData.Parameters,
                    destination: target);
                return Task.CompletedTask;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public Task ReadRoutineStateAsync(IValueContainer target, CancellationToken ct)
        {
            if (_methodExecutionState != null)
            {
                _valueContainerCopier.CopyValues(
                    source: _methodExecutionState.MethodState,
                    destination: target);
                return Task.CompletedTask;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
