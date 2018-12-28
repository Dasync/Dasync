using System;
using System.Collections.Generic;
using System.Threading;
using Dasync.Accessors;
using Dasync.EETypes;
using Dasync.EETypes.Cancellation;

namespace Dasync.ExecutionEngine.Cancellation
{
    public class CancellationTokenSourceRegistry : ICancellationTokenSourceRegistry
    {
        private readonly Dictionary<long, WeakReference<CancellationTokenSource>> _cache =
            new Dictionary<long, WeakReference<CancellationTokenSource>>();

        private readonly INumericIdGenerator _idGenerator;

        public CancellationTokenSourceRegistry(INumericIdGenerator idGenerator)
        {
            _idGenerator = idGenerator;
        }

        public CancellationTokenSourceState Register(CancellationTokenSource source)
        {
            var state = (CancellationTokenSourceState)source.GetState();
            if (state == null)
            {
                state = new CancellationTokenSourceState
                {
                    Id = _idGenerator.NewId(),
                    CancelTime = source.GetCancellationTime()
                };
                source.SetState(state);
                TryAdd(state.Id, source);
            }
            else if (!TryGet(state.Id, out var otherSource))
            {
                TryAdd(state.Id, source);
            }
            return state;
        }

        public bool TryGet(long id, out CancellationTokenSource source)
        {
            WeakReference<CancellationTokenSource> reference;
            lock (_cache)
            {
                _cache.TryGetValue(id, out reference);
            }
            if (reference != null && reference.TryGetTarget(out source))
                return true;
            source = null;
            return false;
        }

        public bool TryAdd(long id, CancellationTokenSource source)
        {
            lock (_cache)
            {
                if (_cache.ContainsKey(id))
                    return false;
                var reference = new WeakReference<CancellationTokenSource>(
                    source, trackResurrection: false);
                _cache.Add(id, reference);
                return true;
            }
        }
    }
}
