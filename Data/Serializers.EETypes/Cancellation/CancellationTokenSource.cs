using System;
using System.Linq;
using System.Threading;
using Dasync.Accessors;
using Dasync.EETypes.Cancellation;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.EETypes.Cancellation
{
    public class CancellationTokenSourceSerializer : IObjectDecomposer, IObjectComposer
    {
        private readonly ICancellationTokenSourceRegistry _registry;

        public CancellationTokenSourceSerializer(ICancellationTokenSourceRegistry registry)
        {
            _registry = registry;
        }

        public IValueContainer Decompose(object value)
        {
            var source = (CancellationTokenSource)value;
            var state = _registry.Register(source);
            return new CancellationTokenSourceContainer
            {
                Id = state.Id,
                CancelTime = state.CancelTime?.ToUniversalTime(),
                LinkedSources = source.GetLinkedSources()
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var values = (CancellationTokenSourceContainer)container;

            if (_registry.TryGet(values.Id, out var result))
                return result;

            if (values.LinkedSources?.Length > 0)
            {
                result = CancellationTokenSource.CreateLinkedTokenSource(
                    values.LinkedSources.Select(x => x.Token).ToArray());
            }
            else if (values.CancelTime.HasValue)
            {
                result = new CancellationTokenSourceWithState();

                var timeout = values.CancelTime.Value - DateTime.UtcNow;
                if (timeout > TimeSpan.Zero)
                {
                    result.CancelAfter(timeout);
                }
                else
                {
                    result.Cancel();
                }
            }

            result.SetState(
                new CancellationTokenSourceState
                {
                    Id = values.Id,
                    CancelTime = values.CancelTime
                });

            _registry.Register(result);

            return result;
        }

        public IValueContainer CreatePropertySet(Type valueType)
        {
            return new CancellationTokenSourceContainer();
        }
    }

    public sealed class CancellationTokenSourceContainer : ValueContainerBase
    {
        public long Id;
        public DateTime? CancelTime;
        public CancellationTokenSource[] LinkedSources;
    }
}
