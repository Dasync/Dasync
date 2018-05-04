using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AzureStorage;
using Dasync.FabricConnector.AzureStorage;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Transitions;
using Dasync.Serialization;
using Dasync.ValueContainer;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Dasync.Fabric.AzureFunctions
{
    public class AzureStorageTransitionCarrier : ITransitionData, ITransitionCarrier
    {
        private static readonly List<string> RoutineRecordPropertiesToRequest =
            new List<string>
            {
                nameof(RoutineRecord.State),
                nameof(RoutineRecord.Continuation)
            };

        private readonly MultipartMessageReader _messageReader;
        private readonly ICloudTable _routinesTable;
        private readonly ICloudTable _servicesTable;
        private readonly ISerializer _serializer;
        private TransitionDescriptor _transitionDescriptor;
        private ServiceId _serviceId;
        private RoutineDescriptor _routineDescriptor;
        private RoutineRecord _routineRecord;

        public AzureStorageTransitionCarrier(
            CloudQueueMessage message,
            ICloudTable routinesTable,
            ICloudTable servicesTable,
            ISerializer serializer)
        {
            _routinesTable = routinesTable;
            _servicesTable = servicesTable;
            _serializer = serializer;
            _messageReader = new MultipartMessageReader(message.AsBytes, serializer);
        }

        public async Task<TransitionDescriptor> GetTransitionDescriptorAsync(CancellationToken ct)
        {
            if (_transitionDescriptor == null)
            {
                if (!_messageReader.TryGetValue("transition", out _transitionDescriptor))
                    throw new InvalidOperationException("No transition info");
            }
            return _transitionDescriptor;
        }

        public Task<ServiceId> GetServiceIdAsync(CancellationToken ct)
        {
            if (_serviceId == null)
                _messageReader.TryGetValue("serviceId", out _serviceId);
            return Task.FromResult(_serviceId);
        }

        public Task<RoutineDescriptor> GetRoutineDescriptorAsync(CancellationToken ct)
        {
            if (_routineDescriptor == null)
                _messageReader.TryGetValue("routine", out _routineDescriptor);
            return Task.FromResult(_routineDescriptor);
        }

        public Task ReadRoutineParametersAsync(IValueContainer target, CancellationToken ct)
        {
            if (_messageReader.TryPopulate("parameters", target))
                return Task.FromResult(true);
            return Task.FromResult(false);
        }

        public async Task ReadRoutineStateAsync(IValueContainer target, CancellationToken ct)
        {
            var transitionDescriptor = await GetTransitionDescriptorAsync(ct);
            if (transitionDescriptor.Type != TransitionType.ContinueRoutine)
                return;

            var routineRecord = await TryLoadRoutineRecordAsync(ct);
            if (routineRecord != null && !string.IsNullOrEmpty(routineRecord.State))
                _serializer.Populate(routineRecord.State, target);
        }

        public Task ReadServiceStateAsync(IValueContainer target, CancellationToken ct)
        {
#warning Read Service state
            throw new NotImplementedException();
        }

        public async Task<List<ContinuationDescriptor>> GetContinuationsAsync(CancellationToken ct)
        {
            if (_messageReader.TryGetValue<ContinuationDescriptor>("continuation", out var descriptor))
                return new List<ContinuationDescriptor>(1) { descriptor };

            var transitionDescriptor = await GetTransitionDescriptorAsync(ct);
            if (transitionDescriptor.Type != TransitionType.ContinueRoutine)
                return null;

            var routineRecord = await TryLoadRoutineRecordAsync(ct);
            if (routineRecord != null && !string.IsNullOrEmpty(routineRecord.Continuation))
            {
                descriptor = _serializer.Deserialize<ContinuationDescriptor>(routineRecord.Continuation);
                return new List<ContinuationDescriptor>(1) { descriptor };
            }

            return null;
        }

        public Task<RoutineResultDescriptor> GetAwaitedResultAsync(CancellationToken ct)
        {
            if (_messageReader.TryGetValue<RoutineResultDescriptor>("result", out var descriptor))
                return Task.FromResult(descriptor);
            return Task.FromResult<RoutineResultDescriptor>(null);
        }

        public async Task SaveStateAsync(SaveStateIntent intent, CancellationToken ct)
        {
            var serviceId = await GetServiceIdAsync(ct);

            if (intent.ServiceState != null)
            {
#warning Save Service state
                //var serviceStateRecord = GetOrCreateServiceStateRecord(intent.ServiceId);
                //serviceStateRecord.StateJson = _serializer.Serialize(intent.ServiceState);
                throw new NotImplementedException();
            }

            if (intent.RoutineState != null || intent.RoutineResult != null)
            {
                var transitionDescriptor = await GetTransitionDescriptorAsync(ct);
                var routineDescriptor = await GetRoutineDescriptorAsync(ct);

                RoutineRecord routineRecord;
                if (transitionDescriptor.Type == TransitionType.InvokeRoutine)
                {
                    routineRecord = new RoutineRecord
                    {
                        PartitionKey = serviceId.ServiceName,
                        RowKey = routineDescriptor.RoutineId
                    };
                }
                else
                {
                    routineRecord = await TryLoadRoutineRecordAsync(ct);
                    if (routineRecord == null)
                        throw new InvalidOperationException("missing routine record");
                }

                if (intent.RoutineResult != null)
                {
                    routineRecord.State = null;
                    routineRecord.Continuation = null;
                    routineRecord.Result = _serializer.SerializeToString(intent.RoutineResult);
                    routineRecord.Status = (int)RoutineStatus.Complete;
                }
                else if (intent.AwaitingRoutine != null)
                {
                    routineRecord.Status = (int)RoutineStatus.Awaiting;
                    routineRecord.AwaitService = intent.AwaitingRoutine.ServiceId?.ServiceName;
                    routineRecord.AwaitMethod = intent.AwaitingRoutine.MethodId?.MethodName;
                    routineRecord.AwaitIntentId = intent.AwaitingRoutine.Id;
                }
                else
                {
                    routineRecord.Status = (int)RoutineStatus.Scheduled;
                }

                if (routineRecord.Status != (int)RoutineStatus.Complete && intent.RoutineState != null)
                {
                    routineRecord.State = _serializer.SerializeToString(intent.RoutineState);
                }

                // Copy over the continuation from the message to the routine record
                //  on first transition, unless routine is already completed.
                if (routineRecord.Status != (int)RoutineStatus.Complete &&
                    string.IsNullOrEmpty(routineRecord.Continuation))
                {
                    if (_messageReader.TryGetPart("continuation", out var stream))
                    {
                        using (stream)
                        {
                            using (var textReader = new StreamReader(stream))
                            {
                                routineRecord.Continuation = textReader.ReadToEnd();
                            }
                        }
                    }
                }

                routineRecord.Method = intent.Routine?.MethodId?.MethodName;

                if (_messageReader.TryGetValue<CallerDescriptor>("caller", out var callerDescriptor))
                {
                    routineRecord.CallerService = callerDescriptor.ServiceId?.ServiceName;
                    routineRecord.CallerMethod = callerDescriptor.Routine?.MethodId?.MethodName;
                    routineRecord.CallerRoutineId = callerDescriptor.Routine?.RoutineId;
                }

                while (true)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(routineRecord.ETag))
                        {
                            try
                            {
                                await _routinesTable.InsertAsync(routineRecord, ct);
                            }
                            catch (TableRowAlreadyExistsException)
                            {
                                throw new ConcurrentTransitionException();
                            }
                        }
                        else
                        {
                            try
                            {
                                await _routinesTable.ReplaceAsync(routineRecord, ct);
                            }
                            catch (TableRowETagMismatchException)
                            {
                                throw new ConcurrentTransitionException();
                            }
                        }
                        break;
                    }
                    catch (TableDoesNotExistException)
                    {
                        await _routinesTable.CreateAsync(ct);
                    }
                }
            }
        }

        private async Task<RoutineRecord> TryLoadRoutineRecordAsync(CancellationToken ct)
        {
            if (_routineRecord == null)
            {
                var serviceId = await GetServiceIdAsync(ct);
                var routineDescriptor = await GetRoutineDescriptorAsync(ct);

                try
                {
                    _routineRecord = await _routinesTable.TryRetrieveAsync<RoutineRecord>(
                        serviceId.ServiceName, routineDescriptor.RoutineId,
                        RoutineRecordPropertiesToRequest, ct);
                }
                catch (TableDoesNotExistException)
                {
                }

                if (_routineRecord != null)
                    routineDescriptor.ETag = _routineRecord.ETag;
            }
            return _routineRecord;
        }
    }
}
