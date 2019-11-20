using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;
using Dasync.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Dasync.Communication.RabbitMQ
{
    public interface IMessageHandler
    {
        IMessageListener StartListeningQueue(IModel channel, string queueName);
    }

    public class RabbitMQMessageHandler : IMessageHandler
    {
        private static readonly ICommunicatorMessage GenericCommunicatorMessage = new CommunicationMessage();

        private readonly ILocalMethodRunner _localTransitionRunner;
        private readonly ISerializerProvider _serializerProvider;
        private readonly IServiceResolver _serviceResolver;
        private HashSet<string> _queueNames = new HashSet<string>();

        public RabbitMQMessageHandler(
            ILocalMethodRunner localTransitionRunner,
            ISerializerProvider serializerProvider,
            IServiceResolver serviceResolver)
        {
            _localTransitionRunner = localTransitionRunner;
            _serializerProvider = serializerProvider;
            _serviceResolver = serviceResolver;
        }

        public IMessageListener StartListeningQueue(IModel channel, string queueName)
        {
            lock (_queueNames)
            {
                if (!_queueNames.Add(queueName))
                    return null;
            }

            channel.QueueDeclare(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += OnMessageReceived;
            var consumerTag = channel.BasicConsume(queueName, autoAck: false, consumer: consumer);

            return new RabbitMQMessageListener(channel, consumerTag);
        }

        private async void OnMessageReceived(object sender, BasicDeliverEventArgs message)
        {
            try
            {
                var consumer = (IBasicConsumer)sender;
                var channel = consumer.Model;

                switch (message.BasicProperties.Type)
                {
                    case MessageTypes.Invoke:
                        await HandleCommandOrQuery(channel, message);
                        break;

                    case MessageTypes.Continue:
                        await HandleResponse(channel, message);
                        break;

                    case MessageTypes.Event:
                        await HandleEvent(channel, message);
                        break;

                    default:
                        channel.BasicNack(message.DeliveryTag, multiple: false, requeue: false);
                        break;
                }
            }
            catch
            {
                // TODO: exception handling
            }
        }

        private async Task HandleCommandOrQuery(IModel channel, BasicDeliverEventArgs message)
        {
            var invocationData = DeserializePayload<MethodInvocationData>(message);
            await _localTransitionRunner.RunAsync(invocationData, GenericCommunicatorMessage);
            channel.BasicAck(message.DeliveryTag, multiple: false);
        }

        private async Task HandleResponse(IModel channel, BasicDeliverEventArgs message)
        {
            var continuationData = DeserializePayload<MethodContinuationData>(message);
            await _localTransitionRunner.ContinueAsync(continuationData, GenericCommunicatorMessage);
            channel.BasicAck(message.DeliveryTag, multiple: false);
        }

        private async Task HandleEvent(IModel channel, BasicDeliverEventArgs message)
        {
            var eventPublishData = DeserializePayload<EventPublishData>(message);

            if (ShouldSkipLocalEvents(message) && _serviceResolver.Resolve(eventPublishData.Service).Definition.Type == ServiceType.Local)
                return;

            // TODO: if there is only 1 subscriber (per this event and per this queue and connection),
            // invoke RunAsync instead of ReactAsync. The latter may even not fan out for multiple subscribers.

            await _localTransitionRunner.ReactAsync(eventPublishData, GenericCommunicatorMessage);
            channel.BasicAck(message.DeliveryTag, multiple: false);
        }

        private T DeserializePayload<T>(BasicDeliverEventArgs message) where T : new()
        {
            Stream stream = new MemoryStream(message.Body, writable: false);

            var encoding = message.BasicProperties.ContentEncoding;
            if (!string.IsNullOrEmpty(encoding))
            {
                if ("gzip".Equals(encoding, StringComparison.OrdinalIgnoreCase))
                {
                    stream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: false);
                }
                else if ("deflate".Equals(encoding, StringComparison.OrdinalIgnoreCase))
                {
                    stream = new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: false);
                }
                else
                {
                    throw new InvalidOperationException($"Unknown Content-Encoding '{encoding}'.");
                }
            }

            var serializer = _serializerProvider.GetSerializer(message.BasicProperties.ContentType);

            using (stream)
            {
                return serializer.Deserialize<T>(stream);
            }
        }

        private bool ShouldSkipLocalEvents(BasicDeliverEventArgs message)
        {
            if (message.BasicProperties.Headers == null || !message.BasicProperties.Headers.TryGetValue("X-Skip-Local", out var value) || value == null)
                return false;

            if (value is string str)
            {
                if (bool.TryParse(str, out var result))
                    return result;
                else
                    return false;
            }

            if (value.GetType() == typeof(bool))
                return (bool)value;

            return false;
        }
    }
}
