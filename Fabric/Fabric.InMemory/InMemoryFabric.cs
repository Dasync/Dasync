using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Fabric;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Transitions;
using Dasync.Serialization;
using static Dasync.Fabric.InMemory.InMemoryDataStore;

namespace Dasync.Fabric.InMemory
{
    public partial class InMemoryFabric : IFabric
    {
        private readonly ITransitionRunner _transitionRunner;
        private string _serializationFormat;
        private readonly ExecutionContext _nonTransitionExecutionContext = ExecutionContext.Capture();

        public InMemoryFabric(ITransitionRunner transitionRunner,
            IInMemoryFabricSerializerFactoryAdvisor serializerFactoryAdvisor)
        {
            _transitionRunner = transitionRunner;
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

            for (; ; )
            {
                var carrier = new TransitionCarrier(this, message);
                var data = carrier;
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
                    await _transitionRunner.RunAsync(carrier, data, ct);
                    break;
                }
                catch (ConcurrentRoutineExecutionException)
                {
                    // re-try
                    continue;
                }
            }
        }

        private ServiceStateRecord GetOrCreateServiceStateRecord(ServiceId serviceId)
        {
            lock (DataStore.Services)
            {
                if (!DataStore.Services.TryGetValue(serviceId.ServiceName, out var record))
                {
                    record = new ServiceStateRecord
                    {
                        Id = serviceId
                    };
                    DataStore.Services.Add(serviceId.ServiceName, record);
                }
                return record;
            }
        }

        private Task SaveStateAsync(SaveStateIntent intent, CancellationToken ct)
        {
            if (intent.ServiceState != null)
            {
                var serviceStateRecord = GetOrCreateServiceStateRecord(intent.ServiceId);
                serviceStateRecord.State = Serializer.SerializeToString(intent.ServiceState);
            }

            if (intent.RoutineState != null || intent.RoutineResult != null)
            {
                var routineRecord = DataStore.GetRoutineRecord(intent.Routine.RoutineId);
                string stateData = null;
                string resultData = null;
                if (intent.RoutineState != null)
                    stateData = Serializer.SerializeToString(intent.RoutineState);
                if (intent.RoutineResult != null)
                    resultData = Serializer.SerializeToString(intent.RoutineResult);

                lock (routineRecord)
                {
                    if (!string.IsNullOrEmpty(intent.Routine.ETag) &&
                        intent.Routine.ETag != routineRecord.ETag)
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
