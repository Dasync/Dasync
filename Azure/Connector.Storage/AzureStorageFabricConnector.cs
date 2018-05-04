using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AzureStorage;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Fabric;
using Dasync.EETypes.Intents;
using Dasync.Serialization;
using Microsoft.WindowsAzure.Storage.Queue;

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

            var transitionDescriptor = new TransitionDescriptor
            {
                Type = TransitionType.InvokeRoutine
            };

            var routineDescriptor = new RoutineDescriptor
            {
                IntentId = intent.Id,
                MethodId = intent.MethodId,
                RoutineId = pregeneratedRoutineId
            };

            var message = PackMessage(
                ("transition", transitionDescriptor),
                ("serviceId", intent.ServiceId),
                ("routine", routineDescriptor),
                ("parameters", intent.Parameters),
                ("continuation", intent.Continuation),
                ("caller", intent.Caller));

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
            var transitionDescriptor = new TransitionDescriptor
            {
                Type = TransitionType.ContinueRoutine
            };

            var message = PackMessage(
                ("transition", transitionDescriptor),
                ("serviceId", intent.Continuation.ServiceId),
                ("routine", intent.Continuation.Routine),
                ("result", intent.Result),
                ("callee", intent.Callee));

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

        private CloudQueueMessage PackMessage(params (string key, object value)[] parts)
        {
            var delimiterId = _idGenerator.NewId();

            if (_serializer is ITextSerializer)
            {
                using (var textWriter = new StringWriter())
                {
                    using (var messageWriter = new MultipartMessageWriter(
                        textWriter, _serializer, delimiterId))
                    {
                        foreach (var (key, value) in parts)
                            if (value != null)
                                messageWriter.Write(key, value);
                    }
                    return new CloudQueueMessage(textWriter.ToString());
                }
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var messageWriter = new MultipartMessageWriter(
                        memoryStream, _serializer, delimiterId))
                    {
                        foreach (var (key, value) in parts)
                            if (value != null)
                                messageWriter.Write(key, value);
                    }
                    var data = memoryStream.ToArray();
                    var message = new CloudQueueMessage(string.Empty);
                    message.SetMessageContent(data);
                    return message;
                }
            }
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
