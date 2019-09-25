using Dasync.EETypes;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;
using Dasync.Proxy;

namespace Dasync.ExecutionEngine.Resolvers
{
    public class EventResolver : IEventResolver
    {
        private readonly IMethodInvokerFactory _methodInvokerFactory;

        public EventResolver(IMethodInvokerFactory methodInvokerFactory)
        {
            _methodInvokerFactory = methodInvokerFactory;
        }

        public bool TryResolve(IServiceDefinition serviceDefinition, EventId eventId, out IEventReference eventReference)
        {
            var eventDefinition = serviceDefinition.FindEvent(eventId.Name);
            if (eventDefinition == null || eventDefinition.IsIgnored)
            {
                eventReference = null;
                return false;
            }

            var delegateMethodInfo = eventDefinition.EventInfo.EventHandlerType.GetMethod("Invoke");
            var invoker = _methodInvokerFactory.Create(delegateMethodInfo);
            eventReference = new EventReference(eventId, eventDefinition, invoker);
            return true;
        }
    }
}
