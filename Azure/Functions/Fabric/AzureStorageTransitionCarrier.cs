using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AzureStorage;
using Dasync.CloudEvents;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Platform;
using Dasync.Fabric.Sample.Base;
using Dasync.FabricConnector.AzureStorage;
using Dasync.Serialization;
using Dasync.ValueContainer;
using Newtonsoft.Json;

namespace Dasync.Fabric.AzureFunctions
{
    public class AzureStorageTransitionCarrier : ITransitionCarrier, ITransitionStateSaver
    {
        private static readonly List<string> RoutineRecordPropertiesToRequest =
            new List<string>
            {
                nameof(RoutineRecord.State),
                nameof(RoutineRecord.Continuation)
            };

        private readonly ICloudTable _routinesTable;
        private readonly ICloudTable _servicesTable;
        private readonly ISerializer _serializer;
        private readonly RoutineEventEnvelope _eventEnvelope;
        private RoutineEventData _routineEventData;
        private RoutineRecord _routineRecord;

        public AzureStorageTransitionCarrier(
            RoutineEventEnvelope eventEnvelope,
            ICloudTable routinesTable,
            ICloudTable servicesTable,
            ISerializer serializer)
        {
            _eventEnvelope = eventEnvelope;
            _routinesTable = routinesTable;
            _servicesTable = servicesTable;
            _serializer = serializer;
        }

        private RoutineEventData EventData
        {
            get
            {
                if (_routineEventData == null && _eventEnvelope.Data != null)
                {
                    _routineEventData = JsonConvert.DeserializeObject<RoutineEventData>(
                        _eventEnvelope.Data, CloudEventsSerialization.JsonSerializerSettings);
                    _eventEnvelope.Data = null; // Free up some memory
                }
                return _routineEventData;
            }
        }

        public Task<TransitionDescriptor> GetTransitionDescriptorAsync(CancellationToken ct)
        {
            TransitionDescriptor result;
            if (_eventEnvelope.EventType == DasyncCloudEventsTypes.InvokeRoutine.Name)
            {
                result = new TransitionDescriptor
                {
                    Type = TransitionType.InvokeRoutine,
                    ETag = _eventEnvelope.ETag
                };
            }
            else if (_eventEnvelope.EventType == DasyncCloudEventsTypes.ContinueRoutine.Name)
            {
                result = new TransitionDescriptor
                {
                    Type = TransitionType.ContinueRoutine,
                    ETag = _eventEnvelope.ETag
                };
            }
            else
            {
                throw new InvalidOperationException($"Unknown event type '{_eventEnvelope.EventType}'.");
            }
            return Task.FromResult(result);
        }

        public Task<ServiceId> GetServiceIdAsync(CancellationToken ct)
        {
            return Task.FromResult(EventData.ServiceId);
        }

        public Task<RoutineDescriptor> GetRoutineDescriptorAsync(CancellationToken ct)
        {
            return Task.FromResult(EventData.Routine);
        }

        public Task ReadRoutineParametersAsync(IValueContainer target, CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(EventData.Parameters))
                _serializer.Populate(EventData.Parameters, target);
            return Task.FromResult(true);
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
            throw new NotImplementedException("Service state is not implemented yet due to non-finalized design.");
        }

        public async Task<List<ContinuationDescriptor>> GetContinuationsAsync(CancellationToken ct)
        {
            if (EventData.Continuation != null)
                return new List<ContinuationDescriptor>(1) { EventData.Continuation };

            var transitionDescriptor = await GetTransitionDescriptorAsync(ct);
            if (transitionDescriptor.Type != TransitionType.ContinueRoutine)
                return null;

            var routineRecord = await TryLoadRoutineRecordAsync(ct);
            if (routineRecord != null && !string.IsNullOrEmpty(routineRecord.Continuation))
            {
                var descriptor = JsonConvert.DeserializeObject<ContinuationDescriptor>(
                    routineRecord.Continuation, CloudEventsSerialization.JsonSerializerSettings);
                return new List<ContinuationDescriptor>(1) { descriptor };
            }

            return null;
        }

        public Task<ResultDescriptor> GetAwaitedResultAsync(CancellationToken ct)
        {
            ResultDescriptor result = null;
            if (EventData.Result != null)
                result = _serializer.Deserialize<ResultDescriptor>(EventData.Result);
            return Task.FromResult(result);
        }

        public async Task SaveStateAsync(SaveStateIntent intent, CancellationToken ct)
        {
            var serviceId = await GetServiceIdAsync(ct);

            if (intent.ServiceState != null)
            {
#warning Save Service state
                //var serviceStateRecord = GetOrCreateServiceStateRecord(intent.ServiceId);
                //serviceStateRecord.StateJson = _serializer.Serialize(intent.ServiceState);
                throw new NotImplementedException("Service state is not implemented yet due to non-finalized design.");
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
                else if (intent.AwaitedRoutine != null)
                {
                    routineRecord.Status = (int)RoutineStatus.Awaiting;
                    routineRecord.AwaitService = intent.AwaitedRoutine.ServiceId?.ServiceName;
                    routineRecord.AwaitMethod = intent.AwaitedRoutine.MethodId?.MethodName;
                    routineRecord.AwaitIntentId = intent.AwaitedRoutine.Id;
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
                    string.IsNullOrEmpty(routineRecord.Continuation) && EventData.Continuation != null)
                {
                    routineRecord.Continuation = JsonConvert.SerializeObject(
                        EventData.Continuation, CloudEventsSerialization.JsonSerializerSettings);
                }

                routineRecord.Method = intent.Routine?.MethodId?.MethodName;

                if (EventData.Caller != null)
                {
                    routineRecord.CallerService = EventData.Caller.ServiceId?.ServiceName;
                    routineRecord.CallerMethod = EventData.Caller.Routine?.MethodId?.MethodName;
                    routineRecord.CallerRoutineId = EventData.Caller.Routine?.RoutineId;
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
