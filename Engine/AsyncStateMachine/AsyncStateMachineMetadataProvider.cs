using System;
using System.Collections.Generic;

namespace Dasync.AsyncStateMachine
{
    public class AsyncStateMachineMetadataProvider : IAsyncStateMachineMetadataProvider
    {
        private readonly IAsyncStateMachineMetadataBuilder _metadataBuilder;

        private readonly Dictionary<Type, AsyncStateMachineMetadata> _metadataMap =
            new Dictionary<Type, AsyncStateMachineMetadata>();

        public AsyncStateMachineMetadataProvider(IAsyncStateMachineMetadataBuilder metadataBuilder)
        {
            _metadataBuilder = metadataBuilder;
        }

        public AsyncStateMachineMetadata GetMetadata(Type asyncStateMachineType)
        {
            lock (_metadataMap)
            {
                if (!_metadataMap.TryGetValue(asyncStateMachineType, out var metadata))
                {
                    metadata = _metadataBuilder.Build(asyncStateMachineType);
                    _metadataMap.Add(asyncStateMachineType, metadata);
                }
                return metadata;
            }
        }
    }
}
