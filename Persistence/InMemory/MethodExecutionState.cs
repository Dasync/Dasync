using System.Collections.Generic;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Persistence;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Persistence.InMemory
{
    public class MethodExecutionState : IMethodExecutionState
    {
        public ISerializer Serializer { get; set; }

        public ServiceId Service { get; set; }

        public PersistedMethodId Method { get; set; }

        public ContinuationDescriptor Continuation { get; set; }

        public string MethodStateData { get; set; }

        public CallerDescriptor Caller { get; set; }

        public Dictionary<string, string> FlowContext { get; set; }

        public ISerializedMethodContinuationState CallerState { get; set; }

        public void ReadMethodState(IValueContainer container) =>
            Serializer.Populate(MethodStateData, container);
    }
}
