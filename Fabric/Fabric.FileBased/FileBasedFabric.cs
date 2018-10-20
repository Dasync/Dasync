using System;
using System.Collections.Async;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dasync.CloudEvents;
using Dasync.EETypes;
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
        private string _serializationFormat;
        private Task _monitorTransitionsTask;
        private CancellationTokenSource _terminationCts;

        public FileBasedFabric(ITransitionRunner transitionRunner,
            IFileBasedFabricSerializerFactoryAdvisor serializerFactoryAdvisor,
            INumericIdGenerator idGenerator)
        {
            _transitionRunner = transitionRunner;

            Directory = Path.GetFullPath(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "data"));
            TransitionsDirectory = Path.Combine(Directory, "transitions");
            RoutinesDirectory = Path.Combine(Directory, "routines");

            var serializerFactory = serializerFactoryAdvisor.Advise();
            _serializationFormat = serializerFactory.Format;
            Serializer = serializerFactory.Create();

            Connector = new FileBasedFabricConnector(TransitionsDirectory, RoutinesDirectory, Serializer, _serializationFormat);
        }

        public IFabricConnector Connector { get; }

        public IFabricConnector GetConnector(ServiceId serviceId) => Connector;

        public string Directory { get; }

        private string TransitionsDirectory { get; }

        private string RoutinesDirectory { get; }

        public ISerializer Serializer { get; }

        public Task InitializeAsync(CancellationToken ct)
        {
            if (!System.IO.Directory.Exists(Directory))
                System.IO.Directory.CreateDirectory(Directory);

            if (!System.IO.Directory.Exists(TransitionsDirectory))
                System.IO.Directory.CreateDirectory(TransitionsDirectory);

            if (!System.IO.Directory.Exists(RoutinesDirectory))
                System.IO.Directory.CreateDirectory(RoutinesDirectory);

            return Task.FromResult(true);
        }

        public Task StartAsync(CancellationToken ct)
        {
            _terminationCts = new CancellationTokenSource();
            _monitorTransitionsTask = Task.Run(() => MonitorTransitions(_terminationCts.Token));
            return Task.FromResult(true);
        }

        public async Task TerminateAsync(CancellationToken ct)
        {
            _terminationCts.Cancel();
            await _monitorTransitionsTask;
        }

        private async Task MonitorTransitions(CancellationToken ct)
        {
            var eventFilesStream = GetEventsFilesStream(TransitionsDirectory);
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
                    var transitionIntentFiles = System.IO.Directory.GetFiles(directory);

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

            for (; ; )
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

            File.Delete(filePath);
        }
    }
}
