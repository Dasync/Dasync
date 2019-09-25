using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Platform;
using Dasync.Fabric.Sample.Base;
using Dasync.Serialization;
using Dasync.ValueContainer;
using static Dasync.Fabric.InMemory.InMemoryDataStore;

namespace Dasync.Fabric.InMemory
{
    partial class InMemoryFabric
    {
        public class TransitionCarrier : ITransitionCarrier, ITransitionStateSaver, ICurrentConnectorProvider
        {
            private readonly InMemoryFabric _fabric;
            private readonly Message _message;
            private PersistedMethodId _methodId;

            public TransitionCarrier(InMemoryFabric fabric, Message message)
            {
                _fabric = fabric;
                _message = message;
            }

            public void Initialize()
            {
                _methodId = GetValueOrDefault<PersistedMethodId>();
                if (_methodId != null)
                {
                    var routineRecord = _fabric.DataStore.GetRoutineRecord(_methodId.RoutineId);
                    if (routineRecord != null)
                    {
                        _methodId.ETag = routineRecord.ETag;
                    }
                }
            }

            public IFabricConnector Connector => _fabric.Connector;

            public Task<ResultDescriptor> GetAwaitedResultAsync(CancellationToken ct)
            {
                var result = GetValueOrDefault<ResultDescriptor>();
                return Task.FromResult(result);
            }

            public Task<List<ContinuationDescriptor>> GetContinuationsAsync(CancellationToken ct)
            {
                List<ContinuationDescriptor> result = null;

                var routineRecord = _fabric.DataStore.GetRoutineRecord(_methodId.RoutineId);

                if (!string.IsNullOrEmpty(routineRecord.Continuation))
                {
                    var continuation = _fabric.Serializer.Deserialize<ContinuationDescriptor>(routineRecord.Continuation);

                    if (continuation != null)
                    {
                        result = new List<ContinuationDescriptor>
                    {
                        continuation
                    };
                    }
                }

                return Task.FromResult(result);
            }

            public Task<PersistedMethodId> GetRoutineDescriptorAsync(CancellationToken ct)
            {
                return Task.FromResult(_methodId);
            }

            public Task<ServiceId> GetServiceIdAsync(CancellationToken ct)
            {
                var result = GetValueOrDefault<ServiceId>();
                return Task.FromResult(result);
            }

            public Task<TransitionDescriptor> GetTransitionDescriptorAsync(CancellationToken ct)
            {
                var result = GetValueOrDefault<TransitionDescriptor>();
                return Task.FromResult(result);
            }

            public Task ReadRoutineParametersAsync(IValueContainer target, CancellationToken ct)
            {
                if (_message.TryGetValue("Parameters", out var data))
                    _fabric.Serializer.Populate(data, target);
                return Task.FromResult(true);
            }

            public Task ReadRoutineStateAsync(IValueContainer target, CancellationToken ct)
            {
                var routineRecord = _fabric.DataStore.GetRoutineRecord(_methodId.RoutineId);
                _fabric.Serializer.Populate(routineRecord.State, target);
                return Task.FromResult(true);
            }

            public Task ReadServiceStateAsync(IValueContainer target, CancellationToken ct)
            {
#warning pre-cache
                var serviceId = GetServiceIdAsync(ct).Result;
                var stateRecord = _fabric.GetOrCreateServiceStateRecord(serviceId);
                _fabric.Serializer.Populate(stateRecord.State, target);
                return Task.FromResult(true);
            }

            public Task SaveStateAsync(SaveStateIntent intent, CancellationToken ct)
                => _fabric.SaveStateAsync(intent, ct);

            private bool TryGetValue<T>(string name, out T value) where T : new()
            {
                if (_message.TryGetValue(name, out var data) && !string.IsNullOrEmpty(data))
                {
                    value = _fabric.Serializer.Deserialize<T>(data);
                    return true;
                }
                else
                {
                    value = default(T);
                    return false;
                }
            }

            private bool TryGetValue<T>(out T value) where T : new()
                => TryGetValue(typeof(T).Name, out value);

            private T GetValueOrDefault<T>(string name) where T : new()
            {
                if (!TryGetValue<T>(name, out var value))
                    value = default(T);
                return value;
            }

            private T GetValueOrDefault<T>() where T : new()
                => GetValueOrDefault<T>(typeof(T).Name);
        }
    }
}
