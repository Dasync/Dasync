using System;
using Dasync.Modeling;

namespace Dasync.EETypes.Resolvers
{
    public interface IEventResolver
    {
        bool TryResolve(IServiceDefinition serviceDefinition, EventId eventId, out IEventReference eventReference);
    }

    public static class EventResolverExtensions
    {
        public static IEventReference Resolve(this IEventResolver resolver, IServiceDefinition serviceDefinition, EventId eventId)
        {
            if (resolver.TryResolve(serviceDefinition, eventId, out var eventReference))
                return eventReference;
            throw new EventResolveException(serviceDefinition.Name, eventId);
        }
    }

    public class EventResolveException : Exception
    {
        public EventResolveException()
            : base("Could not resolve a method.")
        {
        }

        public EventResolveException(string serviceName, EventId eventId)
            : base($"Could not resolve event '{eventId.Name}' in service '{serviceName}'.")
        {
            ServiceName = serviceName;
            EventId = eventId;
        }

        public string ServiceName { get; }

        public EventId EventId { get; }
    }
}
