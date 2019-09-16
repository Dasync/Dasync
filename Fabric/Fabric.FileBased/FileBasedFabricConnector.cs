using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dasync.CloudEvents;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.Fabric.Sample.Base;
using Dasync.Serialization;
using Newtonsoft.Json;

namespace Dasync.Fabric.FileBased
{
    public class FileBasedFabricConnector : IFabricConnector, IFabricConnectorWithConfiguration
    {
        private readonly IUniqueIdGenerator _idGenerator;
        private readonly string _transitionsDirectory;
        private readonly string _routinesDirectory;
        private readonly string _eventsDirectory;
        private readonly string _observersFilePath;
        private readonly ISerializer _serializer;
        private readonly string _serializerFormat;
        private readonly Action<EventDescriptor, EventSubscriberDescriptor> _addEventListener;

        public FileBasedFabricConnector(
            IUniqueIdGenerator idGenerator,
            string transitionsDirectory,
            string routinesDirectory,
            string eventsDirectory,
            Action<EventDescriptor, EventSubscriberDescriptor> addEventListener,
            ISerializer serializer,
            string serializerFormat)
        {
            _idGenerator = idGenerator;
            _transitionsDirectory = transitionsDirectory;
            _routinesDirectory = routinesDirectory;
            _eventsDirectory = eventsDirectory;
            _observersFilePath = Path.Combine(_eventsDirectory, "observers.yaml");
            _addEventListener = addEventListener;
            _serializer = serializer;
            _serializerFormat = serializerFormat;
        }

        public string ConnectorType => "FileBased";

        public object Configuration => new FileBasedFabricConnectorConfiguration
        {
            TransitionsDirectory = _transitionsDirectory,
            RoutinesDirectory = _routinesDirectory,
            EventsDirectory = _eventsDirectory,
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
                //Caller = intent.Caller,
                Continuation = intent.Continuation,
                Parameters = _serializer.SerializeToString(intent.Parameters)
            };

            var eventEnvelope = new RoutineEventEnvelope
            {
                CloudEventsVersion = CloudEventsEnvelope.Version,
                EventType = DasyncCloudEventsTypes.InvokeRoutine.Name,
                EventTypeVersion = DasyncCloudEventsTypes.InvokeRoutine.Version,
                //Source = "/" + (intent.Caller?.ServiceId.ServiceName ?? ""),
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
                ServiceId = intent.ServiceId,
                Routine = intent.Routine,
                //Callee = intent.Callee,
                Result = _serializer.SerializeToString(intent.Result)
            };

            var eventEnvelope = new RoutineEventEnvelope
            {
                CloudEventsVersion = CloudEventsEnvelope.Version,
                EventType = DasyncCloudEventsTypes.ContinueRoutine.Name,
                EventTypeVersion = DasyncCloudEventsTypes.ContinueRoutine.Version,
                //Source = "/" + (intent.Callee?.ServiceId.ServiceName ?? ""),
                EventID = intent.Id.ToString(),
                EventTime = DateTimeOffset.Now,
                EventDeliveryTime = intent.ContinueAt?.ToUniversalTime(),
                ETag = intent.Routine.ETag,
                ContentType = "application/json",
                Data = CloudEventsSerialization.Serialize(eventData)
            };

            var fileName = intent.Id.ToString() + ".json";
            var filePath = Path.Combine(_transitionsDirectory, fileName);
            var content = CloudEventsSerialization.Serialize(eventEnvelope);
            File.WriteAllText(filePath, content, Encoding.UTF8);

            var info = new ActiveRoutineInfo
            {
                RoutineId = intent.Routine.RoutineId
            };

            return Task.FromResult(info);
        }

        public Task SubscribeForEventAsync(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber, IFabricConnector publisherFabricConnector)
        {
            _addEventListener(eventDesc, subscriber);
            return Task.FromResult(0);
        }

        public Task OnEventSubscriberAddedAsync(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber, IFabricConnector subsriberFabricConnector)
        {
            var configuration = (FileBasedFabricConnectorConfiguration)((IFabricConnectorWithConfiguration)subsriberFabricConnector).Configuration;
            var subscriberEventsDirectory = configuration.EventsDirectory;

            var observers = ReadEventObservers();
            if (observers.Add(subscriberEventsDirectory))
                WriteEventObservers(observers);

            return Task.FromResult(0);
        }

        private HashSet<string> ReadEventObservers()
        {
            if (!File.Exists(_observersFilePath))
                return new HashSet<string>();

            var observers = File.ReadAllLines(_observersFilePath)
                .Where(line => !string.IsNullOrWhiteSpace(line) && line.StartsWith("- "))
                .Select(line => line.Substring(2));

            return new HashSet<string>(observers);
        }

        private void WriteEventObservers(HashSet<string> observers)
        {
            var lines = observers.Select(observer => "- " + observer);
            File.WriteAllLines(_observersFilePath, lines);
        }

        public Task PublishEventAsync(RaiseEventIntent intent, CancellationToken ct)
        {
            var eventData = new RoutineEventData
            {
                ServiceId = intent.ServiceId,
                EventId = intent.EventId,
                Parameters = _serializer.SerializeToString(intent.Parameters)
            };

            var eventEnvelope = new RoutineEventEnvelope
            {
                CloudEventsVersion = CloudEventsEnvelope.Version,
                EventType = DasyncCloudEventsTypes.RaiseEvent.Name,
                EventTypeVersion = DasyncCloudEventsTypes.RaiseEvent.Version,
                Source = "/" + (intent.ServiceId.Name ?? ""),
                EventID = intent.Id.ToString(),
                EventTime = DateTimeOffset.Now,
                ContentType = "application/json",
                Data = CloudEventsSerialization.Serialize(eventData)
            };

            var content = CloudEventsSerialization.Serialize(eventEnvelope);

            foreach (var eventsDirectory in ReadEventObservers())
            {
                var fileName = intent.Id.ToString() + ".json";
                var filePath = Path.Combine(eventsDirectory, fileName);
                try
                {
                    File.WriteAllText(filePath, content, Encoding.UTF8);
                }
                catch (IOException)
                {
                }
            }

            return Task.FromResult(0);
        }

        internal void ScheduleRoutineFromEvent(EventSubscriberDescriptor eventSubscriberDescriptor, RoutineEventData raisedEventData)
        {
            var intentId = _idGenerator.NewId();

            var pregeneratedRoutineId = intentId.ToString();

            var routineDescriptor = new RoutineDescriptor
            {
                IntentId = intentId,
                MethodId = eventSubscriberDescriptor.MethodId,
                RoutineId = pregeneratedRoutineId
            };

            var eventData = new RoutineEventData
            {
                ServiceId = eventSubscriberDescriptor.ServiceId,
                Routine = routineDescriptor,
                Caller = new CallerDescriptor
                {
                    Service = raisedEventData.ServiceId
                },
                Parameters = raisedEventData.Parameters
            };

            var eventEnvelope = new RoutineEventEnvelope
            {
                CloudEventsVersion = CloudEventsEnvelope.Version,
                EventType = DasyncCloudEventsTypes.InvokeRoutine.Name,
                EventTypeVersion = DasyncCloudEventsTypes.InvokeRoutine.Version,
                Source = "/" + (raisedEventData.ServiceId?.Name ?? ""),
                EventID = intentId.ToString(),
                EventTime = DateTimeOffset.Now,
                ContentType = "application/json",
                Data = CloudEventsSerialization.Serialize(eventData)
            };

            var fileName = intentId.ToString() + ".json";
            var filePath = Path.Combine(_transitionsDirectory, fileName);
            var content = CloudEventsSerialization.Serialize(eventEnvelope);
            File.WriteAllText(filePath, content, Encoding.UTF8);
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
}
