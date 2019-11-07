using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Eventing;
using Dasync.EETypes.Resolvers;
using Microsoft.Extensions.Configuration;

namespace Dasync.Hosting.AspNetCore.Development
{
    public class EventingMethod : IEventingMethod
    {
        public const string MethodType = "http";

        private readonly IEventSubscriber _eventSubscriber;
        private readonly IUniqueIdGenerator _idGenerator;
        private readonly ICommunicatorProvider _communicatorProvider;
        private readonly IServiceResolver _serviceResolver;

        public EventingMethod(
            IEventSubscriber eventSubscriber,
            IUniqueIdGenerator idGenerator,
            ICommunicatorProvider communicatorProvider,
            IServiceResolver serviceResolver)
        {
            _eventSubscriber = eventSubscriber;
            _idGenerator = idGenerator;
            _communicatorProvider = communicatorProvider;
            _serviceResolver = serviceResolver;
        }

        // DI circular reference
        public ILocalMethodRunner LocalMethodRunner { private get; set; }

        public string Type => MethodType;

        public IEventPublisher CreateEventPublisher(IConfiguration configuration)
        {
            return new EventPublisher(
                _eventSubscriber,
                _idGenerator,
                _communicatorProvider,
                _serviceResolver,
                LocalMethodRunner);
        }
    }
}
