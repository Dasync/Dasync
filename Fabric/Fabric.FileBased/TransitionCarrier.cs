using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dasync.CloudEvents;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Platform;
using Dasync.Fabric.Sample.Base;
using Dasync.Serialization;
using Dasync.ValueContainer;
using Newtonsoft.Json;

namespace Dasync.Fabric.FileBased
{
    partial class FileBasedFabric
    {
        public class TransitionCarrier : ITransitionCarrier, ITransitionStateSaver
        {
            private readonly FileBasedFabric _fabric;
            private readonly RoutineEventEnvelope _eventEnvelope;
            private RoutineEventData _routineEventData;

            public TransitionCarrier(FileBasedFabric fabric, RoutineEventEnvelope eventEnvelope)
            {
                _fabric = fabric;
                _eventEnvelope = eventEnvelope;
            }

            private RoutineEventData EventData
            {
                get
                {
                    if (_routineEventData == null && _eventEnvelope.Data != null)
                    {
                        _routineEventData = JsonConvert.DeserializeObject<RoutineEventData>(_eventEnvelope.Data);
                        _eventEnvelope.Data = null;
                    }
                    return _routineEventData;
                }
            }

            public Task<ResultDescriptor> GetAwaitedResultAsync(CancellationToken ct)
            {
                var result = _fabric.Serializer.Deserialize<ResultDescriptor>(EventData.Result);
                return Task.FromResult(result);
            }

            public Task<List<ContinuationDescriptor>> GetContinuationsAsync(CancellationToken ct)
            {
                var result = new List<ContinuationDescriptor>(1);

                if (_routineEventData.Continuation != null)
                {
                    result.Add(_routineEventData.Continuation);
                }
                else if (FileBasedFabricConnector.TryReadRoutineData(
                    _fabric.RoutinesDirectory, EventData.Routine.RoutineId,
                    out var dataEnvelope, out var eTag) && dataEnvelope.Continuation != null)
                {
                    result.Add(dataEnvelope.Continuation);
                }

                return Task.FromResult(result);
            }

            public Task<RoutineDescriptor> GetRoutineDescriptorAsync(CancellationToken ct)
            {
                var routineDesc = EventData.Routine;
                if (FileBasedFabricConnector.TryGetRoutineETag(
                    _fabric.RoutinesDirectory, routineDesc.RoutineId, out var eTag))
                    routineDesc.ETag = eTag;
                return Task.FromResult(routineDesc);
            }

            public Task<ServiceId> GetServiceIdAsync(CancellationToken ct)
            {
                return Task.FromResult(EventData.ServiceId);
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

            public Task ReadRoutineParametersAsync(IValueContainer target, CancellationToken ct)
            {
                if (!string.IsNullOrEmpty(EventData.Parameters))
                    _fabric.Serializer.Populate(EventData.Parameters, target);
                return Task.FromResult(true);
            }

            public Task ReadRoutineStateAsync(IValueContainer target, CancellationToken ct)
            {
                if (FileBasedFabricConnector.TryReadRoutineData(
                    _fabric.RoutinesDirectory, EventData.Routine.RoutineId,
                    out var dataEnvelope, out var eTag) && dataEnvelope.State != null)
                {
                    _fabric.Serializer.Populate(dataEnvelope.State, target);
                }
                return Task.FromResult(true);
            }

            public Task ReadServiceStateAsync(IValueContainer target, CancellationToken ct)
            {
                throw new NotImplementedException("Service state is not implemented yet due to non-finalized design.");
            }

            public Task SaveStateAsync(SaveStateIntent intent, CancellationToken ct)
            {
                //if (intent.ServiceState != null)
                //{
                //    throw new NotImplementedException("Service state is not implemented yet due to non-finalized design.");
                //}

                if (intent.RoutineState != null || intent.RoutineResult != null)
                {
                    UpsertRoutineData(intent.Routine.RoutineId, intent.Routine.ETag,
                        routineDataEnvelope =>
                        {
                            routineDataEnvelope.ServiceId = intent.ServiceId;

                            if (_routineEventData.Caller != null)
                                routineDataEnvelope.Caller = _routineEventData.Caller;

                            if (intent.RoutineResult != null)
                            {
                                routineDataEnvelope.Status = RoutineStatus.Complete;
                                routineDataEnvelope.Result = _fabric.Serializer.SerializeToString(intent.RoutineResult);
                            }
                            else
                            {
                                routineDataEnvelope.Status = RoutineStatus.Awaiting;
                                routineDataEnvelope.State = _fabric.Serializer.SerializeToString(intent.RoutineState);

                                if (_routineEventData.Continuation != null)
                                    routineDataEnvelope.Continuation = _routineEventData.Continuation;
                            }
                        });
                }

                return Task.FromResult(true);
            }

            private void UpsertRoutineData(string routineId, string expectedETag, Action<RoutineDataEnvelope> updateAction)
            {
                var fileName = routineId + ".json";
                var filePath = Path.Combine(_fabric.RoutinesDirectory, fileName);

                var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096);
#warning need more precise error handling here - what if file is locked?

                using (fileStream)
                {
                    RoutineDataEnvelope routineDataEnvelope;

                    if (fileStream.Length > 0)
                    {
                        var routineETag = new DateTimeOffset(File.GetLastWriteTimeUtc(filePath)).ToString("o");

                        if (!string.IsNullOrEmpty(expectedETag) && expectedETag != routineETag)
                            throw new ConcurrentRoutineExecutionException(
                                new ETagMismatchException());

                        using (var textReader = new StreamReader(fileStream, Encoding.UTF8, true, 512, leaveOpen: true))
                            routineDataEnvelope = (RoutineDataEnvelope)_jsonSerializer.Deserialize(textReader, typeof(RoutineDataEnvelope));

                        fileStream.Position = 0;
                    }
                    else
                    {
                        routineDataEnvelope = new RoutineDataEnvelope();
                    }

                    updateAction(routineDataEnvelope);

                    using (var textWriter = new StreamWriter(fileStream, Encoding.UTF8, 512, leaveOpen: true))
                        _jsonSerializer.Serialize(textWriter, routineDataEnvelope);

                    fileStream.SetLength(fileStream.Position);
                }
            }
        }
    }
}
