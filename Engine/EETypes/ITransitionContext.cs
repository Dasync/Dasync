using System.Collections.Generic;
using Dasync.EETypes.Descriptors;

namespace Dasync.EETypes
{
    public interface ITransitionContext
    {
        ServiceId Service { get; }

        MethodId Method { get; }

        string IntentId { get; }

        CallerDescriptor Caller { get; }

        Dictionary<string, string> FlowContext { get; }
    }

    public static class TransitionContextExtensions
    {
        public static CallerDescriptor CurrentAsCaller(this ITransitionContext context) =>
            new CallerDescriptor(context.Service, context.Method, context.IntentId);
    }
}
