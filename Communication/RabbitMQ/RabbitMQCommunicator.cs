using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Dasync.EETypes.Communication;
using Dasync.Serialization;
using RabbitMQ.Client;

namespace Dasync.Communication.RabbitMQ
{
    public class RabbitMQCommunicator : ICommunicator, IEventPublisher
    {
        private readonly ISerializer _serializer;
        private readonly CommunicationSettings _settings;
        private readonly IModel _channel;

        public RabbitMQCommunicator(
            IConnection connection,
            ISerializer serializer,
            CommunicationSettings settings)
        {
            _serializer = serializer;
            _settings = settings;

            _channel = connection.CreateModel();
            _channel.ConfirmSelect();
        }

        public string Type => RabbitMQCommunicationMethod.MethodType;

        public CommunicationTraits Traits => default;

        public async Task<InvokeRoutineResult> InvokeAsync(
            MethodInvocationData data,
            InvocationPreferences preferences)
        {
            var queueName = _settings.QueueName
                .Replace("{serviceName}", data.Service.Name)
                .Replace("{methodName}", data.Method.Name);

            // TODO: declare once? what's the penalty?
            _channel.QueueDeclare(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var properties = CreateMessageProperties(_channel);
            properties.Type = MessageTypes.Invoke;
            SetIntentId(properties, data.IntentId);
            SetFormat(properties, _serializer);
            properties.Headers.Add("X-Service-Name", data.Service.Name);
            properties.Headers.Add("X-Method-Name", data.Method.Name);

            var payload = SerializePayload(data);

            _channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: payload);

            _channel.WaitForConfirms(); // no async version :(

            return new InvokeRoutineResult
            {
                Outcome = InvocationOutcome.Scheduled
            };
        }

        public async Task<ContinueRoutineResult> ContinueAsync(
            MethodContinuationData data,
            InvocationPreferences preferences)
        {
            var queueName = _settings.QueueName
                .Replace("{serviceName}", data.Service.Name)
                .Replace("{methodName}", data.Method.Name);

            // TODO: declare once? what's the penalty?
            _channel.QueueDeclare(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var properties = CreateMessageProperties(_channel);
            properties.Type = MessageTypes.Continue;
            SetIntentId(properties, data.IntentId);
            SetFormat(properties, _serializer);
            properties.Headers.Add("X-Service-Name", data.Service.Name);
            properties.Headers.Add("X-Method-Name", data.Method.Name);

            // This requires the Message Delay plugin.
            if (data.ContinueAt.HasValue && (Traits & CommunicationTraits.ScheduledDelivery) != default)
            {
                var delay = data.ContinueAt.Value - DateTimeOffset.Now;
                if (delay > TimeSpan.Zero)
                    properties.Headers.Add("x-delay", (int)delay.TotalMilliseconds);
            }

            var payload = SerializePayload(data);

            _channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: payload);

            _channel.WaitForConfirms(); // no async version :(

            return new ContinueRoutineResult
            {
            };
        }

        public async Task PublishAsync(EventPublishData data, PublishPreferences preferences)
        {
            var exchangeName = _settings.ExchangeName
                .Replace("{serviceName}", data.Service.Name)
                .Replace("{eventName}", data.Event.Name);

            // TODO: declare once? what's the penalty?
            _channel.ExchangeDeclare(
                exchangeName,
                type: "fanout",
                durable: true,
                autoDelete: false,
                arguments: null);

            var properties = CreateMessageProperties(_channel);
            properties.Type = MessageTypes.Event;
            SetIntentId(properties, data.IntentId);
            SetFormat(properties, _serializer);
            properties.Headers.Add("X-Service-Name", data.Service.Name);
            properties.Headers.Add("X-Event-Name", data.Event.Name);
            if (preferences.SkipLocalSubscribers)
                properties.Headers.Add("X-Skip-Local", true);

            var payload = SerializePayload(data);

            _channel.BasicPublish(
                exchange: exchangeName,
                routingKey: "",
                basicProperties: properties,
                body: payload);

            _channel.WaitForConfirms(); // no async version :(
        }

        private IBasicProperties CreateMessageProperties(IModel channel)
        {
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            if (_settings.Compress)
                properties.ContentEncoding = "gzip";
            properties.Headers = new Dictionary<string, object>();
            return properties;
        }

        private void SetIntentId(IBasicProperties properties, string intentId)
        {
            properties.MessageId = intentId;

            // Requires the Message Deduplication plugin.
            if ((Traits & CommunicationTraits.MessageDeduplication) != default)
                properties.Headers.Add("x-deduplication-header", intentId);
        }

        private void SetFormat(IBasicProperties properties, ISerializer serializer)
        {
            properties.ContentType = serializer.Format;
        }

        private byte[] SerializePayload(object envelope)
        {
            var bodyStream = new MemoryStream();

            Stream writeStream = bodyStream;
            if (_settings.Compress)
                writeStream = new GZipStream(writeStream, CompressionLevel.Optimal, leaveOpen: true);

            _serializer.Serialize(writeStream, envelope);

            if (_settings.Compress)
                writeStream.Dispose();

            bodyStream.Position = 0;
            return bodyStream.ToArray();
        }
    }
}
