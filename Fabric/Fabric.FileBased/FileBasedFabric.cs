using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.CloudEvents;
using Dasync.DependencyInjection;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Engine;
using Dasync.Fabric.Sample.Base;
using Dasync.Serialization;
using Newtonsoft.Json;

namespace Dasync.Fabric.FileBased
{
    public partial class FileBasedFabric : IFabric
    {
        private static readonly JsonSerializer _jsonSerializer = JsonSerializer.Create(CloudEventsSerialization.JsonSerializerSettings);

        private readonly ITransitionRunner _transitionRunner;
        private readonly IServiceProviderScope _serviceProviderScope;
        private string _serializationFormat;
        private Task _monitorTransitionsTask;
        private Task _monitorEventsTask;
        private CancellationTokenSource _terminationCts;

        private readonly Dictionary<EventDescriptor, List<EventSubscriberDescriptor>> _eventListeners =
            new Dictionary<EventDescriptor, List<EventSubscriberDescriptor>>();

        public FileBasedFabric(
            ITransitionRunner transitionRunner,
            IFileBasedFabricSerializerFactoryAdvisor serializerFactoryAdvisor,
            IUniqueIdGenerator idGenerator,
            IServiceProviderScope serviceProviderScope)
        {
            _transitionRunner = transitionRunner;
            _serviceProviderScope = serviceProviderScope;

            Directory = Path.GetFullPath(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "data"));
            TransitionsDirectory = Path.Combine(Directory, "transitions");
            RoutinesDirectory = Path.Combine(Directory, "routines");
            EventsDirectory = Path.Combine(Directory, "events");

            var serializerFactory = serializerFactoryAdvisor.Advise();
            _serializationFormat = serializerFactory.Format;
            Serializer = serializerFactory.Create();

            Connector = new FileBasedFabricConnector(
                idGenerator,
                TransitionsDirectory,
                RoutinesDirectory,
                EventsDirectory,
                AddEventListener,
                Serializer,
                _serializationFormat);
        }

        public IFabricConnector Connector { get; }

        public IFabricConnector GetConnector(ServiceId serviceId) => Connector;

        public string Directory { get; }

        private string TransitionsDirectory { get; }

        private string RoutinesDirectory { get; }

        private string EventsDirectory { get; }

        public ISerializer Serializer { get; }

        public Task InitializeAsync(CancellationToken ct)
        {
            if (!System.IO.Directory.Exists(Directory))
                System.IO.Directory.CreateDirectory(Directory);

            if (!System.IO.Directory.Exists(TransitionsDirectory))
                System.IO.Directory.CreateDirectory(TransitionsDirectory);

            if (!System.IO.Directory.Exists(RoutinesDirectory))
                System.IO.Directory.CreateDirectory(RoutinesDirectory);

            if (!System.IO.Directory.Exists(EventsDirectory))
                System.IO.Directory.CreateDirectory(EventsDirectory);

            return Task.FromResult(true);
        }

        public Task StartAsync(CancellationToken ct)
        {
            _terminationCts = new CancellationTokenSource();
            _monitorTransitionsTask = Task.Run(() => MonitorTransitions(_terminationCts.Token));
            _monitorEventsTask = Task.Run(() => MonitorEvents(_terminationCts.Token));
            return Task.FromResult(true);
        }

        public async Task TerminateAsync(CancellationToken ct)
        {
            _terminationCts.Cancel();
            await _monitorTransitionsTask;
            await _monitorEventsTask;
        }

        private async Task MonitorTransitions(CancellationToken ct)
        {
            var eventFilesStream = GetEventsFilesStream(TransitionsDirectory);
            await eventFilesStream.ParallelForEachAsync(
                filePath => ProcessEventAsync(filePath, ct),
                ct);
        }

        private async Task MonitorEvents(CancellationToken ct)
        {
            var eventFilesStream = GetEventsFilesStream(EventsDirectory);
            await eventFilesStream.ParallelForEachAsync(
                filePath => ProcessEventAsync(filePath, ct),
                ct);
        }

        private IAsyncEnumerable<string> GetEventsFilesStream(string directory) =>
            new AsyncEnumerable<string>(async yield =>
            {
                DateTime minCreateTime = DateTime.MinValue;

                while (!yield.CancellationToken.IsCancellationRequested)
                {
                    var transitionIntentFiles = System.IO.Directory.GetFiles(directory, "*.json");

                    var batchMinCreateTime = DateTime.MinValue;
                    var returnedFiles = 0;

                    foreach (var filePath in transitionIntentFiles)
                    {
                        if (_terminationCts.IsCancellationRequested)
                            break;

                        try
                        {
                            var createTime = File.GetCreationTime(filePath);
                            if (createTime > minCreateTime)
                                await yield.ReturnAsync(filePath);

                            returnedFiles++;
                            if (createTime > batchMinCreateTime)
                                batchMinCreateTime = createTime;
                        }
                        catch (IOException)
                        {
                            continue;
                        }
                    }

                    if (returnedFiles > 0)
                    {
                        minCreateTime = batchMinCreateTime;
                    }
                    else
                    {
                        await Task.Delay(50);
                    }
                }
            });

        private async Task ProcessEventAsync(string filePath, CancellationToken ct)
        {
#warning Need to exclusively lock the file 
            var json = File.ReadAllText(filePath);
            var eventEnvelope = JsonConvert.DeserializeObject<RoutineEventEnvelope>(
                json, CloudEventsSerialization.JsonSerializerSettings);
            json = null; // save memory

            if (eventEnvelope.EventDeliveryTime.HasValue && eventEnvelope.EventDeliveryTime > DateTimeOffset.UtcNow)
            {
                await Task.Delay(eventEnvelope.EventDeliveryTime.Value - DateTimeOffset.UtcNow);
            }

            if (eventEnvelope.EventType == DasyncCloudEventsTypes.RaiseEvent.Name)
            {
                await RaiseEventAsync(eventEnvelope, ct);
            }
            else
            {
                await RunRoutineAsync(eventEnvelope, ct);
            }

            File.Delete(filePath);
        }

        private async Task RunRoutineAsync(RoutineEventEnvelope eventEnvelope, CancellationToken ct)
        {
            for (; ; )
            {
                using (_serviceProviderScope.New())
                {
                    var carrier = new TransitionCarrier(this, eventEnvelope);

                    //var transitionInfo = await data.GetTransitionDescriptorAsync(ct);
                    //if (transitionInfo.Type == TransitionType.InvokeRoutine ||
                    //    transitionInfo.Type == TransitionType.ContinueRoutine)
                    //{
                    //    var routineDescriptor = await data.GetRoutineDescriptorAsync(ct);

                    //    if (!string.IsNullOrEmpty(transitionInfo.ETag) &&
                    //        transitionInfo.ETag != routineDescriptor.ETag)
                    //    {
                    //        // Ignore - stale duplicate message
                    //        return;
                    //    }
                    //}

                    try
                    {
                        await _transitionRunner.RunAsync(carrier, ct);
                        break;
                    }
                    catch (ConcurrentRoutineExecutionException)
                    {
                        // re-try
                        continue;
                    }
                }
            }
        }

        public void AddEventListener(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber)
        {
            lock (_eventListeners)
            {
                if (!_eventListeners.TryGetValue(eventDesc, out var listeners))
                    _eventListeners[eventDesc] = listeners = new List<EventSubscriberDescriptor>();
                listeners.Add(subscriber);
            }
        }

        public IEnumerable<EventSubscriberDescriptor> GetEventListeners(EventDescriptor eventDesc)
        {
            if (_eventListeners.TryGetValue(eventDesc, out var listeners))
                return listeners;
            return Enumerable.Empty<EventSubscriberDescriptor>();
        }

        private async Task RaiseEventAsync(RoutineEventEnvelope eventEnvelope, CancellationToken ct)
        {
            var routineEventData = JsonConvert.DeserializeObject<RoutineEventData>(eventEnvelope.Data);

            var eventDescriptor = new EventDescriptor
            {
                Service = routineEventData.ServiceId,
                Event = routineEventData.EventId
            };

            foreach (var eventSubscriberDescriptor in GetEventListeners(eventDescriptor))
            {
                ((FileBasedFabricConnector)Connector).ScheduleRoutineFromEvent(eventSubscriberDescriptor, routineEventData);
            }
        }
    }
}
