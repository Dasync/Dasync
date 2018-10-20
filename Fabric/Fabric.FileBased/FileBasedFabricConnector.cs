using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dasync.CloudEvents;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.Fabric.Sample.Base;
using Dasync.Serialization;
using Newtonsoft.Json;

namespace Dasync.Fabric.FileBased
{
    public class FileBasedFabricConnector : IFabricConnector, IFabricConnectorWithConfiguration
    {
        private readonly string _transitionsDirectory;
        private readonly string _routinesDirectory;
        private readonly ISerializer _serializer;
        private readonly string _serializerFormat;

        public FileBasedFabricConnector(
            string transitionsDirectory,
            string routinesDirectory,
            ISerializer serializer,
            string serializerFormat)
        {
            _transitionsDirectory = transitionsDirectory;
            _routinesDirectory = routinesDirectory;
            _serializer = serializer;
            _serializerFormat = serializerFormat;
        }

        public string ConnectorType => "FileBased";

        public object Configuration => new FileBasedFabricConnectorConfiguration
        {
            TransitionsDirectory = _transitionsDirectory,
            RoutinesDirectory = _routinesDirectory,
            SerializerFormat = _serializerFormat
        };

        public Task<ActiveRoutineInfo> ScheduleRoutineAsync(
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

            var fileName = intent.Id.ToString() + ".json";
            var filePath = Path.Combine(_transitionsDirectory, fileName);
            var content = CloudEventsSerialization.Serialize(eventEnvelope);
            File.WriteAllText(filePath, content, Encoding.UTF8);

            var info = new ActiveRoutineInfo
            {
                RoutineId = pregeneratedRoutineId
            };

            return Task.FromResult(info);
        }

        public async Task<ActiveRoutineInfo> PollRoutineResultAsync(
            ActiveRoutineInfo info, CancellationToken ct)
        {
            TaskResult routineResult = null;

            if (TryReadRoutineData(_routinesDirectory, info.RoutineId, out var dataEnvelope, out var eTag))
            {
                if (dataEnvelope.Status == RoutineStatus.Complete)
                {
                    routineResult = _serializer.Deserialize<TaskResult>(dataEnvelope.Result);
                }
            }

            return new ActiveRoutineInfo
            {
                RoutineId = info.RoutineId,
                Result = routineResult,
                ETag = eTag
            };
        }

        public Task<ActiveRoutineInfo> ScheduleContinuationAsync(
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

            var fileName = intent.Id.ToString() + ".json";
            var filePath = Path.Combine(_transitionsDirectory, fileName);
            var content = CloudEventsSerialization.Serialize(eventEnvelope);
            File.WriteAllText(filePath, content, Encoding.UTF8);

            var info = new ActiveRoutineInfo
            {
                RoutineId = intent.Continuation.Routine.RoutineId
            };

            return Task.FromResult(info);
        }

        internal static bool TryReadRoutineData(string directory, string routineId, out RoutineDataEnvelope dataEnvelope, out string eTag)
        {
            var fileName = routineId + ".json";
            var filePath = Path.Combine(directory, fileName);

            if (File.Exists(filePath))
            {
                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        eTag = new DateTimeOffset(File.GetLastWriteTimeUtc(filePath)).ToString("o");
                        dataEnvelope = JsonConvert.DeserializeObject<RoutineDataEnvelope>(
                            json, CloudEventsSerialization.JsonSerializerSettings);
                        return true;
                    }
                    catch (IOException) // File is being used by another process.
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                }
            }

            eTag = null;
            dataEnvelope = null;
            return false;
        }

        internal static bool TryGetRoutineETag(string directory, string routineId, out string eTag)
        {
            var fileName = routineId + ".json";
            var filePath = Path.Combine(directory, fileName);

            if (File.Exists(filePath))
            {
                eTag = new DateTimeOffset(File.GetLastWriteTimeUtc(filePath)).ToString("o");
                return true;
            }
            else
            {
                eTag = null;
                return false;
            }
        }
    }
}
