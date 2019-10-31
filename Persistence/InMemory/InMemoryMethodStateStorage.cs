using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;

namespace Dasync.Persistence.InMemory
{
    public class InMemoryMethodStateStorage : IMethodStateStorage
    {
        private readonly ISerializer _serializer;
        private readonly Dictionary<string, StorageEntry> _entryMap = new Dictionary<string, StorageEntry>();

        public InMemoryMethodStateStorage(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public Task WriteStateAsync(
            ServiceId serviceId,
            PersistedMethodId methodId,
            MethodExecutionState state)
        {
            var serializedState = _serializer.SerializeToString(state.MethodState);
            var serializedContinuation = state.Continuation != null ? _serializer.SerializeToString(state.Continuation) : null;
            var serializedFlowContext = state.FlowContext?.Count > 0 ? _serializer.SerializeToString(state.FlowContext) : null;

            var expectedETag = methodId.ETag;
            var intentId = methodId.IntentId;

            lock (_entryMap)
            {
                if (!_entryMap.TryGetValue(intentId, out var entry))
                {
                    entry = new StorageEntry();
                    _entryMap.Add(intentId, entry);
                }
                else if (!string.IsNullOrEmpty(expectedETag) && entry.ETag != expectedETag)
                {
                    throw new ETagMismatchException(expectedETag, entry.ETag);
                }

                entry.ETag = DateTimeOffset.UtcNow.Ticks.ToString();
                entry["ServiceId"] = state.Service.Clone();
                entry["MethodId"] = state.Method.Clone();
                entry["Caller"] = state.Caller?.Clone();
                entry["State"] = serializedState;
                entry["Continuation"] = serializedContinuation;
                entry["FlowContext"] = serializedFlowContext;

                if (state.ContinuationState != null)
                {
                    entry["Continuation:Format"] = state.ContinuationState.Format;
                    entry["Continuation:State"] = state.ContinuationState.State;
                }
            }

            return Task.CompletedTask;
        }

        public Task<MethodExecutionState> ReadStateAsync(
            ServiceId serviceId,
            PersistedMethodId methodId,
            CancellationToken ct)
        {
            MethodExecutionState executionState;
            string serializedFlowContext;
            string serializedContinuation;
            string continuationStateFormat;
            byte[] continuationStateData;

            lock (_entryMap)
            {
                if (!_entryMap.TryGetValue(methodId.IntentId, out var entry))
                    throw new StateNotFoundException(serviceId, methodId);

                executionState = new MethodExecutionState
                {
                    Service = serviceId,
                    Method = methodId,
                    Caller = entry.TryGetValue("Caller", out var callerObj) ? callerObj as CallerDescriptor : null,
                    MethodState = new SerializedValueContainer((string)entry["State"], _serializer)
                };
                executionState.Method.ETag = entry.ETag;

                serializedFlowContext = entry.TryGetValue("FlowContext", out var flowContextObj) ? flowContextObj as string : null;
                serializedContinuation = entry.TryGetValue("Continuation", out var continuationObj) ? continuationObj as string : null;
                continuationStateFormat = entry.TryGetValue("Continuation:Format", out var continuationStateFormatObj) ? continuationStateFormatObj as string : null;
                continuationStateData = entry.TryGetValue("Continuation:State", out var continuationStateDataObj) ? continuationStateDataObj as byte[] : null;
            }

            if (!string.IsNullOrEmpty(serializedFlowContext))
                executionState.FlowContext = _serializer.Deserialize<Dictionary<string, string>>(serializedFlowContext);

            if (!string.IsNullOrEmpty(serializedContinuation))
                executionState.Continuation = _serializer.Deserialize<ContinuationDescriptor>(serializedContinuation);

            if (continuationStateData?.Length > 0)
            {
                executionState.ContinuationState = new SerializedMethodContinuationState
                {
                    Format = continuationStateFormat,
                    State = continuationStateData
                };
            }

            return Task.FromResult(executionState);
        }

        public Task WriteResultAsync(ServiceId serviceId, MethodId methodId, string intentId, TaskResult result)
        {
            var serializedTaskResult = _serializer.SerializeToString(result);

            var expectedETag = (methodId as PersistedMethodId)?.ETag;

            lock (_entryMap)
            {
                if (!_entryMap.TryGetValue(intentId, out var entry))
                {
                    entry = new StorageEntry();
                    _entryMap.Add(intentId, entry);
                }
                else if (!string.IsNullOrEmpty(expectedETag) && entry.ETag != expectedETag)
                {
                    throw new ETagMismatchException(expectedETag, entry.ETag);
                }

                entry["ServiceId"] = serviceId.Clone();
                entry["MethodId"] = methodId.Clone();
                entry["Result"] = serializedTaskResult;
                entry.ETag = DateTimeOffset.UtcNow.Ticks.ToString();
            }

            return Task.CompletedTask;
        }

        public Task<TaskResult> TryReadResultAsync(ServiceId serviceId, MethodId methodId, string intentId, Type resultValueType, CancellationToken ct)
        {
            object serializedResultObj = null;

            lock (_entryMap)
            {
                if (!_entryMap.TryGetValue(intentId, out var entry) ||
                    !entry.TryGetValue("Result", out serializedResultObj))
                    return Task.FromResult<TaskResult>(null);
            }

            // TODO: use 'resultValueType'
            var result = _serializer.Deserialize<TaskResult>((string)serializedResultObj);
            return Task.FromResult(result);
        }

        private class StorageEntry : Dictionary<string, object>
        {
            public string ETag { get; set; }
        }
    }
}
