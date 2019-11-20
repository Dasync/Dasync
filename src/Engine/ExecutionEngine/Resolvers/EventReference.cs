using Dasync.EETypes;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;
using Dasync.Proxy;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.Resolvers
{
    public class EventReference : IEventReference
    {
        private readonly IMethodInvoker _invoker;

        public EventReference(EventId id, IEventDefinition definition, IMethodInvoker invoker)
        {
            Id = id;
            Definition = definition;
            _invoker = invoker;
        }

        public EventId Id { get; }

        public IEventDefinition Definition { get; }

        public IValueContainer CreateParametersContainer() => _invoker.CreateParametersContainer();
    }
}