using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.Fabric.Sample.Base;
using Dasync.Serialization;
using static Dasync.Fabric.InMemory.InMemoryDataStore;

namespace Dasync.Fabric.InMemory
{
    public class InMemoryFabricConnector : IFabricConnector, IFabricConnectorWithConfiguration
    {
        private readonly InMemoryDataStore _dataStore;
        private readonly ISerializer _serializer;
        private readonly string _serializerFormat;

        public InMemoryFabricConnector(
            InMemoryDataStore dataStore,
            ISerializer serializer,
            string serializerFormat)
        {
            _dataStore = dataStore;
            _serializer = serializer;
            _serializerFormat = serializerFormat;
        }

        public string ConnectorType => "InMemory";

        public object Configuration => new InMemoryFabricConnectorConfiguration
        {
            DataStoreId = _dataStore.Id,
            SerializerFormat = _serializerFormat
        };

        public Task<ActiveRoutineInfo> ScheduleRoutineAsync(
            ExecuteRoutineIntent intent, CancellationToken ct)
        {
#warning Need to send message first, then create routine record.

            var routineId = Interlocked.Increment(ref _dataStore.RoutineCounter);

            var routineRecord = new RoutineStateRecord
            {
                ETag = DateTime.UtcNow.Ticks.ToString("X16"),
                Id = routineId.ToString(),
                Completion = new TaskCompletionSource<string>(),
                Continuation = intent.Continuation == null ? null : _serializer.SerializeToString(intent.Continuation)
            };

            lock (_dataStore.Routines)
            {
                _dataStore.Routines.Add(routineRecord.Id, routineRecord);
            }

            var transitionDescriptor = new TransitionDescriptor
            {
                Type = TransitionType.InvokeRoutine,
                ETag = routineRecord.ETag
            };

            var methodId = intent.Method.CopyTo(
                new PersistedMethodId
                {
                    IntentId = intent.Id,
                    RoutineId = routineRecord.Id,
                    ETag = routineRecord.ETag
                });

            var message = new Message
            {
                //["IntentId"] = _serializer.Serialize(intent.Id),
                [nameof(TransitionDescriptor)] = _serializer.SerializeToString(transitionDescriptor),
                [nameof(ServiceId)] = _serializer.SerializeToString(intent.Service),
                [nameof(PersistedMethodId)] = _serializer.SerializeToString(methodId),
                ["Parameters"] = _serializer.SerializeToString(intent.Parameters)
            };

            _dataStore.ScheduleMessage(message);

            var info = new ActiveRoutineInfo
            {
                RoutineId = routineRecord.Id
            };

            return Task.FromResult(info);
        }

        public async Task<ActiveRoutineInfo> PollRoutineResultAsync(
            ActiveRoutineInfo info, CancellationToken ct)
        {
            var routineRecord = _dataStore.GetRoutineRecord(info.RoutineId);
            var resultData = await routineRecord.Completion.Task;
            var result = _serializer.Deserialize<TaskResult>(resultData);
            return new ActiveRoutineInfo
            {
                RoutineId = routineRecord.Id,
                Result = result
            };
        }

        public Task<ActiveRoutineInfo> ScheduleContinuationAsync(
            ContinueRoutineIntent intent, CancellationToken ct)
        {
            var transitionDescriptor = new TransitionDescriptor
            {
                Type = TransitionType.ContinueRoutine,
                ETag = intent.Method.ETag
            };

            var message = new Message
            {
                //["IntentId"] = _serializer.Serialize(intent.Id),
                [nameof(TransitionDescriptor)] = _serializer.SerializeToString(transitionDescriptor),
                [nameof(ServiceId)] = _serializer.SerializeToString(intent.Service),
                [nameof(PersistedMethodId)] = _serializer.SerializeToString(intent.Method),
                [nameof(ResultDescriptor)] = _serializer.SerializeToString(intent.Result),
                DeliverAt = intent.ContinueAt
            };

            _dataStore.ScheduleMessage(message);

            var info = new ActiveRoutineInfo
            {
                RoutineId = intent.Method.RoutineId
            };

            return Task.FromResult(info);
        }

        public Task SubscribeForEventAsync(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber, IFabricConnector publisherFabricConnector)
        {
            _dataStore.AddEventListener(eventDesc, subscriber);
            return Task.FromResult(0);
        }

        public Task OnEventSubscriberAddedAsync(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber, IFabricConnector subsriberFabricConnector)
        {
            return Task.FromResult(0);
        }

        public Task PublishEventAsync(RaiseEventIntent intent, CancellationToken ct)
        {
            var message = new Message
            {
                IsEvent = true,
                [nameof(ServiceId)] = _serializer.SerializeToString(intent.Service),
                [nameof(EventId)] = _serializer.SerializeToString(intent.Event),
                ["Parameters"] = _serializer.SerializeToString(intent.Parameters)
            };

            InMemoryDataStore.BroadcastMessage(message);

            return Task.FromResult(0);
        }

        public Task RegisterTriggerAsync(RegisterTriggerIntent intent, CancellationToken ct)
        {
            _dataStore.AddTrigger(intent.TriggerId, intent.ValueType);
            return Task.FromResult(0);
        }

        public Task ActivateTriggerAsync(ActivateTriggerIntent intent, CancellationToken ct)
        {
            _dataStore.ActivateTrigger(intent.TriggerId, intent.Value);
            return Task.FromResult(0);
        }

        public Task SubscribeToTriggerAsync(SubscribeToTriggerIntent intent, CancellationToken ct)
        {
            _dataStore.SubscribeToTrigger(
                intent.TriggerId,
                taskResult =>
                {
                    var transitionDescriptor = new TransitionDescriptor
                    {
                        Type = TransitionType.ContinueRoutine,
                        ETag = intent.Continuation.Method.ETag
                    };

                    var resultDescriptor = new ResultDescriptor
                    {
                        TaskId = intent.TriggerId,
                        Result = taskResult
                    };

                    var message = new Message
                    {
                        [nameof(TransitionDescriptor)] = _serializer.SerializeToString(transitionDescriptor),
                        [nameof(ServiceId)] = _serializer.SerializeToString(intent.Continuation.Service),
                        [nameof(PersistedMethodId)] = _serializer.SerializeToString(intent.Continuation.Method),
                        [nameof(ResultDescriptor)] = _serializer.SerializeToString(resultDescriptor),
                        DeliverAt = intent.Continuation.ContinueAt?.ToUniversalTime()
                    };

                    _dataStore.ScheduleMessage(message);
                });

            return Task.FromResult(0);
        }
    }
}
