using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.EETypes.Platform;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.Transitions
{
    internal class TransitionCarrier : ITransitionCarrier, ISerializedMethodContinuationState
    {
        private readonly IInvocationData _invocationData;
        private readonly IMethodInvocationData _methodInvocationData;
        private readonly IMethodContinuationData _methodContinuationData;
        private ISerializedMethodContinuationState _continuationState;
        private IMethodExecutionState _methodExecutionState;

        public CallerDescriptor Caller { get; private set; }

        public TransitionCarrier(IMethodInvocationData methodInvocationData, ISerializedMethodContinuationState continuationState)
        {
            _invocationData = methodInvocationData;
            _methodInvocationData = methodInvocationData;
            _continuationState = continuationState;
            Caller = methodInvocationData.Caller;
        }

        public TransitionCarrier(IMethodContinuationData routineContinuationData)
        {
            _invocationData = routineContinuationData;
            _methodContinuationData = routineContinuationData;
        }

        string ISerializedMethodContinuationState.Format
        {
            get => _continuationState?.Format;
            set { if (_continuationState != null) _continuationState.Format = value; }
        }

        byte[] ISerializedMethodContinuationState.State
        {
            get => _continuationState?.State;
            set { if (_continuationState != null) _continuationState.State = value; }
        }

        public void SetMethodExecutionState(IMethodExecutionState methodExecutionState)
        {
            _methodExecutionState = methodExecutionState;
            _continuationState = methodExecutionState.CallerState;
            Caller = _methodExecutionState.Caller;
        }

        public string ResultTaskId => _methodContinuationData?.TaskId;

        public TaskResult ReadResult(Type expectedResultValueType)
        {
            if (_methodContinuationData == null)
                throw new InvalidOperationException();
            return _methodContinuationData.ReadResult(expectedResultValueType);
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
                return _methodInvocationData.ReadInputParameters(target);
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
                _methodExecutionState.ReadMethodState(target);
                return Task.CompletedTask;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
