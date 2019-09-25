using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.DependencyInjection;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Intents;
using Dasync.Fabric.Sample.Base;
using Dasync.Serialization;
using static Dasync.Fabric.InMemory.InMemoryDataStore;

namespace Dasync.Fabric.InMemory
{
    public partial class InMemoryFabric : IFabric
    {
        private readonly ITransitionRunner _transitionRunner;
        private string _serializationFormat;
        private readonly ExecutionContext _nonTransitionExecutionContext = ExecutionContext.Capture();
        private readonly IUniqueIdGenerator _uniqueIdGenerator;
        private readonly IServiceProviderScope _serviceProviderScope;

        public InMemoryFabric(ITransitionRunner transitionRunner,
            IInMemoryFabricSerializerFactoryAdvisor serializerFactoryAdvisor,
            IUniqueIdGenerator numericIdGenerator,
            IServiceProviderScope serviceProviderScope)
        {
            _transitionRunner = transitionRunner;
            _uniqueIdGenerator = numericIdGenerator;
            _serviceProviderScope = serviceProviderScope;

            DataStore = InMemoryDataStore.Create(ScheduleMessage);
            var serializerFactory = serializerFactoryAdvisor.Advise();
            _serializationFormat = serializerFactory.Format;
            Serializer = serializerFactory.Create();
            Connector = new InMemoryFabricConnector(DataStore, Serializer, _serializationFormat);
        }

        public IFabricConnector Connector { get; }

        public IFabricConnector GetConnector(ServiceId serviceId) => Connector;

        public InMemoryDataStore DataStore { get; }

        public ISerializer Serializer { get; }

        public Task InitializeAsync(CancellationToken ct)
        {
            return Task.FromResult(true);
        }

        public Task StartAsync(CancellationToken ct)
        {
            return Task.FromResult(true);
        }

        public Task TerminateAsync(CancellationToken ct)
        {
            return Task.FromResult(true);
        }

        private void ScheduleMessage(Message message)
        {
            ExecutionContext.Run(
                // .NET Framework complains about having the execution
                // context being used more than once if not copied.
                _nonTransitionExecutionContext.CreateCopy(),
                _ => RunMessageInBackground(message),
                state: null);
        }

        private async void RunMessageInBackground(Message message)
        {
            if (message.DeliverAt.HasValue && message.DeliverAt > DateTime.UtcNow)
            {
                await Task.Delay(message.DeliverAt.Value - DateTime.UtcNow);
            }
            else
            {
                await Task.Yield();
            }

            var ct = CancellationToken.None;

            if (message.IsEvent)
            {
                var serviceId = Serializer.Deserialize<ServiceId>(message[nameof(ServiceId)]);
                var eventId = Serializer.Deserialize<EventId>(message[nameof(EventId)]);
                var eventDesc = new EventDescriptor { Event = eventId, Service = serviceId };
                var subscribers = DataStore.GetEventSubscribers(eventDesc);

                foreach (var subscriber in subscribers)
                {
                    var routineId = Interlocked.Increment(ref DataStore.RoutineCounter);

                    var routineRecord = new RoutineStateRecord
                    {
                        ETag = DateTime.UtcNow.Ticks.ToString("X16"),
                        Id = routineId.ToString(),
                        Completion = new TaskCompletionSource<string>()
                    };

                    lock (DataStore.Routines)
                    {
                        DataStore.Routines.Add(routineRecord.Id, routineRecord);
                    }

                    var transitionDescriptor = new TransitionDescriptor
                    {
                        Type = TransitionType.InvokeRoutine,
                        ETag = routineRecord.ETag
                    };

                    var methodId = subscriber.Method.CopyTo(
                        new PersistedMethodId
                        {
                            IntentId = _uniqueIdGenerator.NewId(),
                            RoutineId = routineRecord.Id,
                            ETag = routineRecord.ETag
                        });

                    var invokeRoutineMessage = new Message
                    {
                        //["IntentId"] = _serializer.Serialize(intent.Id),
                        [nameof(TransitionDescriptor)] = Serializer.SerializeToString(transitionDescriptor),
                        [nameof(ServiceId)] = Serializer.SerializeToString(subscriber.Service),
                        [nameof(PersistedMethodId)] = Serializer.SerializeToString(methodId),
                        ["Parameters"] = message["Parameters"]
                    };

                    DataStore.ScheduleMessage(invokeRoutineMessage);
                }
            }
            else
            {
                for (; ; )
                {
                    using (_serviceProviderScope.New())
                    {
                        var carrier = new TransitionCarrier(this, message);
                        carrier.Initialize();

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
        }

        private ServiceStateRecord GetOrCreateServiceStateRecord(ServiceId serviceId)
        {
            lock (DataStore.Services)
            {
                if (!DataStore.Services.TryGetValue(serviceId.Name, out var record))
                {
                    record = new ServiceStateRecord
                    {
                        Id = serviceId
                    };
                    DataStore.Services.Add(serviceId.Name, record);
                }
                return record;
            }
        }

        private Task SaveStateAsync(SaveStateIntent intent, CancellationToken ct)
        {
            //if (intent.ServiceState != null)
            //{
            //    var serviceStateRecord = GetOrCreateServiceStateRecord(intent.ServiceId);
            //    serviceStateRecord.State = Serializer.SerializeToString(intent.ServiceState);
            //}

            if (intent.RoutineState != null || intent.RoutineResult != null)
            {
                var routineRecord = DataStore.GetRoutineRecord(intent.Method.RoutineId);
                string stateData = null;
                string resultData = null;
                if (intent.RoutineResult == null && intent.RoutineState != null)
                    stateData = Serializer.SerializeToString(intent.RoutineState);
                if (intent.RoutineResult != null)
                    resultData = Serializer.SerializeToString(intent.RoutineResult);

                lock (routineRecord)
                {
                    if (!string.IsNullOrEmpty(intent.Method.ETag) &&
                        intent.Method.ETag != routineRecord.ETag)
                        throw new ConcurrentRoutineExecutionException(
                            new ETagMismatchException());

                    routineRecord.ETag = DateTime.UtcNow.Ticks.ToString("X16");
                    routineRecord.State = stateData;
                    routineRecord.Result = resultData;
                }

                if (intent.RoutineResult != null)
                    routineRecord.Completion.SetResult(resultData);
            }

            return Task.FromResult(true);
        }
    }
}
