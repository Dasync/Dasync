using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Communication.Http;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Eventing;
using Dasync.EETypes.Resolvers;
using Dasync.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Dasync.Hosting.AspNetCore.Development
{
    public class BackgroundEventSubscriber : IHostedService
    {
        private readonly IEventSubscriber _eventSubscriber;
        private readonly IServiceResolver _serviceResolver;
        private readonly ICommunicatorProvider _communicatorProvider;
        private readonly IDefaultSerializerProvider _defaultSerializerProvider;
        private readonly ISerializerProvider _serializerProvider;
        private readonly Dictionary<ServiceId, EventingHttpClient> _clientMap = new Dictionary<ServiceId, EventingHttpClient>();

        private bool _stopRequested;
        private Task _periodicSubscribeTask;

        public BackgroundEventSubscriber(
            IEventSubscriber eventSubscriber,
            IServiceResolver serviceResolver,
            ICommunicatorProvider communicatorProvider,
            IDefaultSerializerProvider defaultSerializerProvider,
            ISerializerProvider serializerProvider,
            EventingMethod eventingMethod,
            ILocalMethodRunner localMethodRunner)
        {
            _eventSubscriber = eventSubscriber;
            _serviceResolver = serviceResolver;
            _communicatorProvider = communicatorProvider;
            _defaultSerializerProvider = defaultSerializerProvider;
            _serializerProvider = serializerProvider;

            // DI circular reference
            eventingMethod.LocalMethodRunner = localMethodRunner;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stopRequested = false;
            _periodicSubscribeTask = SubscribePeriodically();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopRequested = true;

            if (_periodicSubscribeTask != null)
            {
                await _periodicSubscribeTask;
                _periodicSubscribeTask = null;
            }
        }

        private async Task SubscribePeriodically()
        {
            while (!_stopRequested)
            {
                var subscribeTasks = new List<Task>();

                var events = _eventSubscriber.SubscribedEvents.ToList();
                foreach (var eventDesc in events)
                {
                    if (!IsExternal(eventDesc))
                        continue;

                    var client = GetEventingClient(eventDesc.Service);
                    var subscribers = _eventSubscriber.GetSubscribers(eventDesc);

                    subscribeTasks.Add(client.SubscribeAsync(eventDesc, subscribers));
                }

                try
                {
                    await Task.WhenAll(subscribeTasks);
                }
                catch
                {
                }

                await Task.Delay(5_000);
            }
        }

        private bool IsExternal(EventDescriptor eventDesc)
        {
            return !_serviceResolver.TryResolve(eventDesc.Service, out var serviceReference)
                || serviceReference.Definition.Type == Modeling.ServiceType.External;
        }

        private EventingHttpClient GetEventingClient(ServiceId serviceId)
        {
            lock (_clientMap)
            {
                if (_clientMap.TryGetValue(serviceId, out var client))
                    return client;
                client = CreateEventingClient(serviceId);
                _clientMap.Add(serviceId, client);
                return client;
            }
        }

        private EventingHttpClient CreateEventingClient(ServiceId serviceId)
        {
            var serviceRef = _serviceResolver.Resolve(serviceId);

            var configuration = _communicatorProvider.GetCommunicatorConfiguration(serviceId, default);
            var settings = new HttpCommunicatorSettings();
            configuration.Bind(settings);

            var serializer = string.IsNullOrEmpty(settings.Serializer)
                ? _defaultSerializerProvider.DefaultSerializer
                : _serializerProvider.GetSerializer(settings.Serializer);
            var compressPayload = settings.Compress ?? false;
            var urlTemplate = HttpCommunicationMethod.GetUrlTemplate(settings);

            var schemeDelimiterIndex = urlTemplate.IndexOf("://");
            var firstSegmentIndex = urlTemplate.IndexOf('/', schemeDelimiterIndex > 0 ? schemeDelimiterIndex + 3 : 0);
            var address = firstSegmentIndex > 0 ? urlTemplate.Substring(0, firstSegmentIndex) : urlTemplate;

            address = address.Replace("{serviceName}", serviceRef.Definition.Name);

            var urlBase = address + "/dev/events";

            return new EventingHttpClient(serializer, urlBase);
        }
    }
}
