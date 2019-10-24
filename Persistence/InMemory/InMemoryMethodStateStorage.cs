using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;
using Dasync.ValueContainer;

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
            ITransitionContext context,
            IValueContainer methodState,
            ContinuationDescriptor continuation,
            ISerializedMethodContinuationState callerState)
        {
            var serializedState = _serializer.SerializeToString(methodState);
            var serializedContinuation = continuation != null ? _serializer.SerializeToString(continuation) : null;
            var serializedFlowContext = context.FlowContext?.Count > 0 ? _serializer.SerializeToString(context.FlowContext) : null;

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
                entry["ServiceId"] = context.Service.Clone();
                entry["MethodId"] = context.Method.Clone();
                entry["Caller"] = context.Caller?.Clone();
                entry["State"] = serializedState;
                entry["Continuation"] = serializedContinuation;
                entry["FlowContext"] = serializedFlowContext;
                entry["Caller"] = context.Caller?.Clone();

                if (callerState != null)
                {
                    entry["CallerContentType"] = callerState.ContentType;
                    entry["CallerState"] = callerState.State;
                }
            }

            return Task.CompletedTask;
        }

        public Task<IMethodExecutionState> ReadStateAsync(
            ServiceId serviceId,
            PersistedMethodId methodId,
            CancellationToken ct)
        {
            MethodExecutionState state;
            string serializedFlowContext;
            string serializedContinuation;
            string callerContentType;
            byte[] callerState;

            lock (_entryMap)
            {
                if (!_entryMap.TryGetValue(methodId.IntentId, out var entry))
                    throw new StateNotFoundException(serviceId, methodId);

                state = new MethodExecutionState
                {
                    Service = serviceId,
                    Method = methodId,
                    Caller = entry.TryGetValue("Caller", out var callerObj) ? callerObj as CallerDescriptor : null,
                    MethodStateData = (string)entry["State"],
                    Serializer = _serializer,
                };
                state.Method.ETag = entry.ETag;

                serializedFlowContext = entry.TryGetValue("FlowContext", out var flowContextObj) ? flowContextObj as string : null;
                serializedContinuation = entry.TryGetValue("Continuation", out var continuationObj) ? continuationObj as string : null;
                callerContentType = entry.TryGetValue("CallerContentType", out var callerContentTypeObj) ? callerContentTypeObj as string : null;
                callerState = entry.TryGetValue("CallerState", out var callerStateObj) ? callerStateObj as byte[] : null;
            }

            if (!string.IsNullOrEmpty(serializedFlowContext))
                state.FlowContext = _serializer.Deserialize<Dictionary<string, string>>(serializedFlowContext);

            if (!string.IsNullOrEmpty(serializedContinuation))
                state.Continuation = _serializer.Deserialize<ContinuationDescriptor>(serializedContinuation);

            if (callerState?.Length > 0)
            {
                state.CallerState = new SerializedMethodContinuationState
                {
                    ContentType = callerContentType,
                    State = callerState
                };
            }

            return Task.FromResult<IMethodExecutionState>(state);
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
