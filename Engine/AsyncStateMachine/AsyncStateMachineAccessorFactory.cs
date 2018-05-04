using System.Collections.Generic;

namespace Dasync.AsyncStateMachine
{
    public class AsyncStateMachineAccessorFactory : IAsyncStateMachineAccessorFactory
    {
        private readonly Dictionary<AsyncStateMachineMetadata, IAsyncStateMachineAccessor> _accessorMap =
            new Dictionary<AsyncStateMachineMetadata, IAsyncStateMachineAccessor>();

        public IAsyncStateMachineAccessor Create(AsyncStateMachineMetadata metadata)
        {
            lock (_accessorMap)
            {
                if (!_accessorMap.TryGetValue(metadata, out var accessor))
                {
                    accessor = new AsyncStateMachineAccessor(metadata);
                    _accessorMap.Add(metadata, accessor);
                }
                return accessor;
            }
        }
    }
}
