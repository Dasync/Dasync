using System.Collections.Generic;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes.Communication
{
    public interface IInvocationData
    {
        string IntentId { get; }

        CallerDescriptor Caller { get; }

        Dictionary<string, string> FlowContext { get; }
    }
}
