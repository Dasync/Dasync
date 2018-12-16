using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AzureStorage;
using Dasync.CloudEvents;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.Fabric.Sample.Base;
using Dasync.Serialization;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Dasync.FabricConnector.AzureStorage
{
    public class AzureStorageFabricConnector : IFabricConnector
    {
        private static readonly List<string> RoutineRecordPropertiesForPolling =
            new List<string>
            {
                nameof(RoutineRecord.Status),
                nameof(RoutineRecord.Result)
            };

        private readonly ServiceId _serviceId;
        private readonly INumericIdGenerator _idGenerator;
        private readonly ICloudQueue _transitionsQueue;
        private readonly ICloudTable _routinesTable;
        private readonly ISerializer _serializer;

        public AzureStorageFabricConnector(
            ServiceId serviceId,
            INumericIdGenerator idGenerator,
            ICloudQueue transitionsQueue,
            ICloudTable routinesTable,
            ISerializer serializer)
        {
            _serviceId = serviceId;
            _idGenerator = idGenerator;
            _transitionsQueue = transitionsQueue;
            _routinesTable = routinesTable;
            _serializer = serializer;
        }

        public async Task<ActiveRoutineInfo> ScheduleRoutineAsync(
            ExecuteRoutineIntent intent, CancellationToken ct)
        {
            var pregeneratedRoutineId = intent.Id.ToString();

            var routineDescriptor = new RoutineDescriptor
            {
                IntentId = intent.Id,
                MethodId = intent.MethodId,
                RoutineId = pregeneratedRoutineId
            };

            var eventData = new RoutineEventData
            {
                ServiceId = intent.ServiceId,
                Routine = routineDescriptor,
                Caller = intent.Caller,
                Continuation = intent.Continuation,
                Parameters = _serializer.SerializeToString(intent.Parameters)
            };

            var eventEnvelope = new RoutineEventEnvelope
            {
                CloudEventsVersion = CloudEventsEnvelope.Version,
                EventType = DasyncCloudEventsTypes.InvokeRoutine.Name,
                EventTypeVersion = DasyncCloudEventsTypes.InvokeRoutine.Version,
                Source = "/" + (intent.Caller?.ServiceId.ServiceName ?? ""),
                EventID = intent.Id.ToString(),
                EventTime = DateTimeOffset.Now,
                ContentType = "application/json",
                Data = CloudEventsSerialization.Serialize(eventData)
            };

            var message = new CloudQueueMessage(
                JsonConvert.SerializeObject(eventEnvelope,
                    CloudEventsSerialization.JsonSerializerSettings));

            while (true)
            {
                try
                {
                    await _transitionsQueue.AddMessageAsync(message, null, null, ct);
                    break;
                }
                catch (QueueDoesNotExistException)
                {
                    await _transitionsQueue.CreateAsync(ct);
                }
            }

            return new ActiveRoutineInfo
            {
                RoutineId = pregeneratedRoutineId
            };
        }

        public async Task<ActiveRoutineInfo> ScheduleContinuationAsync(
            ContinueRoutineIntent intent, CancellationToken ct)
        {
            var eventData = new RoutineEventData
            {
                ServiceId = intent.Continuation.ServiceId,
                Routine = intent.Continuation.Routine,
                Callee = intent.Callee,
                Result = _serializer.SerializeToString(intent.Result)
            };

            var eventEnvelope = new RoutineEventEnvelope
            {
                CloudEventsVersion = CloudEventsEnvelope.Version,
                EventType = DasyncCloudEventsTypes.ContinueRoutine.Name,
                EventTypeVersion = DasyncCloudEventsTypes.ContinueRoutine.Version,
                Source = "/" + (intent.Callee?.ServiceId.ServiceName ?? ""),
                EventID = intent.Id.ToString(),
                EventTime = DateTimeOffset.Now,
                EventDeliveryTime = intent.Continuation.ContinueAt?.ToUniversalTime(),
                ETag = intent.Continuation.Routine.ETag,
                ContentType = "application/json",
                Data = CloudEventsSerialization.Serialize(eventData)
            };

            var message = new CloudQueueMessage(
                JsonConvert.SerializeObject(eventEnvelope,
                    CloudEventsSerialization.JsonSerializerSettings));

            TimeSpan? delay = null;
            if (intent.Continuation.ContinueAt.HasValue)
            {
                delay = intent.Continuation.ContinueAt.Value.ToUniversalTime() - DateTime.UtcNow;
                if (delay <= TimeSpan.Zero)
                    delay = null;
            }

            while (true)
            {
                try
                {
                    await _transitionsQueue.AddMessageAsync(message, null, delay, ct);
                    break;
                }
                catch (QueueDoesNotExistException)
                {
                    await _transitionsQueue.CreateAsync(ct);
                }
            }

            return new ActiveRoutineInfo
            {
                RoutineId = intent.Continuation.Routine.RoutineId,
                ETag = intent.Continuation.Routine.ETag
            };
        }

        public async Task<ActiveRoutineInfo> PollRoutineResultAsync(
            ActiveRoutineInfo info, CancellationToken ct)
        {
            RoutineRecord routineRecord;
            try
            {
                routineRecord = await _routinesTable.TryRetrieveAsync<RoutineRecord>(
                    _serviceId.ServiceName, info.RoutineId, RoutineRecordPropertiesForPolling, ct);
            }
            catch (TableDoesNotExistException)
            {
                routineRecord = null;
            }

            if (routineRecord != null)
            {
                TaskResult routineResult = null;

                if (routineRecord.Status == (int)RoutineStatus.Complete &&
                    !string.IsNullOrEmpty(routineRecord.Result))
                    routineResult = _serializer.Deserialize<TaskResult>(routineRecord.Result);

                info = new ActiveRoutineInfo
                {
                    ETag = routineRecord.ETag,
                    RoutineId = info.RoutineId,
                    Result = routineResult
                };
            }

            return info;
        }

        public Task SubscribeForEventAsync(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber, IFabricConnector publisherFabricConnector)
        {
            throw new NotImplementedException();
        }

        public Task OnEventSubscriberAddedAsync(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber, IFabricConnector subsriberFabricConnector)
        {
            throw new NotImplementedException();
        }

        public Task PublishEventAsync(RaiseEventIntent intent, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task RegisterTriggerAsync(RegisterTriggerIntent intent, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task ActivateTriggerAsync(ActivateTriggerIntent intent, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeToTriggerAsync(SubscribeToTriggerIntent intent, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }

    public class AzureStorageFabricConnectorWithConfiguration
        : AzureStorageFabricConnector, IFabricConnectorWithConfiguration
    {
        public AzureStorageFabricConnectorWithConfiguration(
            ServiceId serviceId,
            INumericIdGenerator idGenerator,
            ICloudQueue transitionsQueue,
            ICloudTable routinesTable,
            ISerializer serializer,
            AzureStorageFabricConnectorConfiguration originalConfiguration)
            : base(serviceId, idGenerator, transitionsQueue, routinesTable, serializer)
        {
            Configuration = originalConfiguration;
        }

        public string ConnectorType => "AzureStorage";

        public object Configuration { get; }
    }
}
