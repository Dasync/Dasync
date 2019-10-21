using System.Collections.Generic;
using Dasync.EETypes.Descriptors;
using Dasync.ValueContainer;

namespace Dasync.EETypes.Persistence
{
    public interface IMethodExecutionState
    {
        ServiceId Service { get; }

        PersistedMethodId Method { get; }

        Dictionary<string, string> FlowContext { get; }

        void ReadMethodState(IValueContainer container);

        ContinuationDescriptor Continuation { get; }

        ISerializedMethodContinuationState CallerState { get; }

        CallerDescriptor Caller { get; }
    }
}
