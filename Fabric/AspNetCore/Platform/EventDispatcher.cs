using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.AspNetCore.Communication;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Platform;
using Dasync.Modeling;

namespace Dasync.AspNetCore.Platform
{
    public interface IEventDispatcher
    {
        void OnSubscriberAdded(EventDescriptor eventDesc, ServiceId subscriber);

        Task PublishEvent(RaiseEventIntent intent);

        IReadOnlyCollection<EventSubscriberDescriptor> GetEventHandlers(EventDescriptor eventDesc);
    }

    public class EventDispatcher : IEventDispatcher, IEventSubscriber
    {
        private readonly ICommunicationModel _communicationModel;
        private readonly IPlatformHttpClientProvider _platformHttpClientProvider;
        private readonly ITransitionUserContext _transitionUserContext;

        private readonly Dictionary<EventDescriptor, List<EventSubscriberDescriptor>> _eventHandlers =
            new Dictionary<EventDescriptor, List<EventSubscriberDescriptor>>();

        private readonly Dictionary<EventDescriptor, HashSet<ServiceId>> _eventListeners =
            new Dictionary<EventDescriptor, HashSet<ServiceId>>();

        public EventDispatcher(
            ICommunicationModel communicationModel,
            IPlatformHttpClientProvider platformHttpClientProvider,
            ITransitionUserContext transitionUserContext)
        {
            _communicationModel = communicationModel;
            _platformHttpClientProvider = platformHttpClientProvider;
            _transitionUserContext = transitionUserContext;
        }

        public IReadOnlyCollection<EventSubscriberDescriptor> GetEventHandlers(EventDescriptor eventDesc)
        {
            _eventHandlers.TryGetValue(eventDesc, out var handlers);
            return handlers;
        }

        public void Subscribe(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber)
        {
            var publisherServiceDefinition = GetServiceDefinition(eventDesc.Service);

            lock (_eventHandlers)
            {
                if (!_eventHandlers.TryGetValue(eventDesc, out var handlers))
                    _eventHandlers[eventDesc] = handlers = new List<EventSubscriberDescriptor>();
                handlers.Add(subscriber);
            }

            if (publisherServiceDefinition.Type == ServiceType.Local)
            {
                OnSubscriberAdded(eventDesc, subscriber.Service);
            }
            if (publisherServiceDefinition.Type == ServiceType.External)
            {
                SubscribePeriodicallyInBackground(eventDesc, subscriber.Service, publisherServiceDefinition);
            }
        }

        public void OnSubscriberAdded(EventDescriptor eventDesc, ServiceId subscriber)
        {
            lock (_eventListeners)
            {
                if (!_eventListeners.TryGetValue(eventDesc, out var subscribers))
                    _eventListeners[eventDesc] = subscribers = new HashSet<ServiceId>();
                subscribers.Add(subscriber);
            }
        }

        public async Task PublishEvent(RaiseEventIntent intent)
        {
            var eventDesc = new EventDescriptor { Service = intent.Service, Event = intent.Event };
            if (_eventListeners.TryGetValue(eventDesc, out var subscribers))
            {
                foreach (var subscriber in subscribers)
                {
                    PublishEventInBackground(intent, subscriber);
                }
            }
        }

        private IServiceDefinition GetServiceDefinition(ServiceId serviceId)
        {
            var serviceName = serviceId.Proxy ?? serviceId.Name;

            var serviceDefinition = _communicationModel.Services.FirstOrDefault(d => d.Name == serviceName);
            if (serviceDefinition == null)
                throw new ArgumentException($"Service '{serviceName}' is not registered.");

            return serviceDefinition;
        }

        private IServiceDefinition GetOrFakeServiceDefinition(ServiceId serviceId)
        {
            var serviceName = serviceId.Proxy ?? serviceId.Name;

            var serviceDefinition = _communicationModel.Services.FirstOrDefault(d => d.Name == serviceName);
            if (serviceDefinition != null)
                return serviceDefinition;

            return new UnknownExternalServiceDefinition(serviceName, _communicationModel);
        }

        private async void SubscribePeriodicallyInBackground(
            EventDescriptor eventDesc,
            ServiceId subscriber,
            IServiceDefinition publisherServiceDefinition)
        {
            var subscriberServiceDefinition = GetServiceDefinition(subscriber);

            while (true)
            {
                var client = _platformHttpClientProvider.GetClient(publisherServiceDefinition);

                try
                {
                    await client.SubscribeToEvent(eventDesc, subscriber, publisherServiceDefinition);
                    await Task.Delay(TimeSpan.FromMinutes(2));
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }

        private async void PublishEventInBackground(
            RaiseEventIntent intent,
            ServiceId subscriber)
        {
            while (true)
            {
                var subscriberServiceDefinition = GetOrFakeServiceDefinition(subscriber);
                var client = _platformHttpClientProvider.GetClient(subscriberServiceDefinition);

                try
                {
                    await client.PublishEvent(intent, subscriberServiceDefinition, _transitionUserContext.Current);
                    return;
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    continue;
                }
            }
        }
    }
}
