using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Configuration;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Eventing;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Dasync.Communication.RabbitMQ
{
    public class RabbitMQMessageListiningMethod : IMessageListeningMethod
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IMessageHandler _messageHandler;
        private readonly IEventSubscriber _eventSubscriber;
        private readonly IEventIdProvider _eventIdProvider;
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;
        private readonly ICommunicationModelConfiguration _communicationModelConfiguration;

        public RabbitMQMessageListiningMethod(
            IConnectionManager connectionManager,
            IMessageHandler messageHandler,
            IEventSubscriber eventSubscriber,
            IEventIdProvider eventIdProvider,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver,
            ICommunicationModelConfiguration communicationModelConfiguration)
        {
            _connectionManager = connectionManager;
            _messageHandler = messageHandler;
            _eventSubscriber = eventSubscriber;
            _eventIdProvider = eventIdProvider;
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;
            _communicationModelConfiguration = communicationModelConfiguration;
        }

        public string Type => RabbitMQCommunicationMethod.MethodType;

        public Task<IEnumerable<IMessageListener>> StartListeningAsync(
            IConfiguration configuration,
            IServiceDefinition serviceDefinition,
            IDictionary<IMethodDefinition, IConfiguration> methodConfigMap,
            CancellationToken ct)
        {
            var resultListeners = new List<IMessageListener>();

            var baseConnectionSettings = new ConnectionSettings();
            configuration.Bind(baseConnectionSettings);

            var baseListenerSettings = RabbitMQCommunicationMethod.CreateMethodsDefaultSettings();
            configuration.Bind(baseListenerSettings);

            IConnection baseConnection = null;
            IModel baseChannel = null;

            if (!baseListenerSettings.QueueName.Contains("{methodName}"))
            {
                baseConnection = _connectionManager.GetConnection(baseConnectionSettings);
                baseChannel = baseConnection.CreateModel();
                baseChannel.ConfirmSelect();
            }

            foreach (var methodDefinition in serviceDefinition.Methods)
            {
                if (methodDefinition.IsIgnored)
                    continue;

                var connection = baseConnection;
                var channel = baseChannel;
                CommunicationSettings listenerSettings;
                ConnectionSettings connectionSettings;

                if (methodConfigMap.TryGetValue(methodDefinition, out var methodConfiguration))
                {
                    connectionSettings = new ConnectionSettings();
                    methodConfiguration.Bind(connectionSettings);

                    listenerSettings = RabbitMQCommunicationMethod.CreateMethodsDefaultSettings();
                    methodConfiguration.Bind(listenerSettings);
                }
                else
                {
                    connectionSettings = baseConnectionSettings;
                    listenerSettings = baseListenerSettings;
                }

                if (connection == null)
                {
                    connection = _connectionManager.GetConnection(connectionSettings);
                    channel = connection.CreateModel();
                    channel.ConfirmSelect();
                }

                var queueName = listenerSettings.QueueName
                    .Replace("{serviceName}", serviceDefinition.Name)
                    .Replace("{methodName}", methodDefinition.Name);

                var listener = _messageHandler.StartListeningQueue(channel, queueName);

                if (listener != null)
                    resultListeners.Add(listener);
            }

            return Task.FromResult<IEnumerable<IMessageListener>>(resultListeners);
        }

        public Task<IEnumerable<IMessageListener>> StartListeningAsync(
            IConfiguration configuration,
            IServiceDefinition serviceDefinition,
            IDictionary<IEventDefinition, IConfiguration> eventConfigMap,
            CancellationToken ct)
        {
            // TODO: there are many scenarios that are not supported by the following code.
            // For example, exchange's connection is different from services' queues.

            var baseConnectionSettings = new ConnectionSettings();
            configuration.Bind(baseConnectionSettings);

            var baseConnection = _connectionManager.GetConnection(baseConnectionSettings);
            var baseChannel = baseConnection.CreateModel();

            foreach (var eventDefinition in serviceDefinition.Events)
            {
                if (!eventConfigMap.TryGetValue(eventDefinition, out var eventConfiguration))
                    eventConfiguration = configuration;

                var listenerSettings = RabbitMQCommunicationMethod.CreateEventsDefaultSettings();
                eventConfiguration.Bind(listenerSettings);

                var exchangeName = listenerSettings.ExchangeName
                    .Replace("{serviceName}", serviceDefinition.Name)
                    .Replace("{eventName}", eventDefinition.Name);

                // TODO: declare once? what's the penalty?
                baseChannel.ExchangeDeclare(
                    exchangeName,
                    type: "fanout",
                    durable: true,
                    autoDelete: false,
                    arguments: null);

                var eventDesc = new EventDescriptor
                {
                    Service = new ServiceId { Name = serviceDefinition.Name },
                    Event = _eventIdProvider.GetId(eventDefinition.EventInfo)
                };

                foreach (var subscriber in _eventSubscriber.GetSubscribers(eventDesc))
                {
                    if (!_serviceResolver.TryResolve(subscriber.Service, out var subscriberServiceReference))
                        continue;

                    if (!_methodResolver.TryResolve(subscriberServiceReference.Definition, subscriber.Method, out var subscriberMethodReference))
                        continue;

                    var subscriberMethodConfiguration = _communicationModelConfiguration.GetMethodConfiguration(subscriberMethodReference.Definition, "communication");

                    var subscriberSettings = RabbitMQCommunicationMethod.CreateMethodsDefaultSettings();
                    subscriberMethodConfiguration.Bind(subscriberSettings);

                    var subscriberQueueName = subscriberSettings.QueueName
                        .Replace("{serviceName}", subscriberServiceReference.Definition.Name)
                        .Replace("{methodName}", subscriberMethodReference.Definition.Name);

                    // TODO: declare once? what's the penalty?
                    baseChannel.QueueBind(
                        queue: subscriberQueueName,
                        exchange: exchangeName,
                        routingKey: "",
                        arguments: null);
                }
            }

            // No direct listeners to RabbitMQ Exchanges, just configure exchange-queue bindings.

            // TODO: if events (or subscribers) use a different connection,
            // need to create a queue and a listener in that RabbitMQ instance.
            return Task.FromResult<IEnumerable<IMessageListener>>(Array.Empty<IMessageListener>());
        }
    }
}
