using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Resolvers;
using Dasync.ExecutionEngine.Modeling;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.ExecutionEngine.Communication
{
    public class EventPublisherProvider : IEventPublisherProvider
    {
        private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
        private readonly IConfiguration _configuration;
        private readonly IServiceResolver _serviceResolver;
        private readonly IEventResolver _eventResolver;
        private readonly IExternalCommunicationModel _externalCommunicationModel;
        private readonly Dictionary<string, IEventingMethod> _eventingMethods;
        private readonly Dictionary<object, IEventPublisher> _publisherMap = new Dictionary<object, IEventPublisher>();

        public EventPublisherProvider(
            ICommunicationSettingsProvider communicationSettingsProvider,
            IEnumerable<IConfiguration> safeConfiguration,
            IServiceResolver serviceResolver,
            IEventResolver eventResolver,
            IExternalCommunicationModel externalCommunicationModel,
            IEnumerable<IEventingMethod> eventingMethods)
        {
            _communicationSettingsProvider = communicationSettingsProvider;
            _serviceResolver = serviceResolver;
            _eventResolver = eventResolver;
            _externalCommunicationModel = externalCommunicationModel;

            _configuration = safeConfiguration.FirstOrDefault()?.GetSection("dasync")
                ?? (IConfiguration)new ConfigurationRoot(Array.Empty<IConfigurationProvider>());

            _eventingMethods = eventingMethods.ToDictionary(m => m.Type, m => m, StringComparer.OrdinalIgnoreCase);
        }

        public IEventPublisher GetPublisher(ServiceId serviceId, EventId eventId)
        {
            if (_eventingMethods.Count == 0)
                throw new CommunicationMethodNotFoundException("There are no communication methods registered.");

            IServiceDefinition serviceDefinition;
            IEventDefinition eventDefinition;

            if (_serviceResolver.TryResolve(serviceId, out var serviceRef))
            {
                serviceDefinition = serviceRef.Definition;
                eventDefinition = _eventResolver.Resolve(serviceDefinition, eventId).Definition;
            }
            else
            {
                throw new ServiceResolveException(serviceId);
            }

            lock (_publisherMap)
            {
                if (_publisherMap.TryGetValue(eventDefinition, out var cachedCommunicator))
                    return cachedCommunicator;
            }

            EventCommunicationSettings methodCommunicationSettings =
                _communicationSettingsProvider.GetEventSettings(eventDefinition);

            var communicationType = methodCommunicationSettings.CommunicationType;

            IEventingMethod eventingMethod;
            if (string.IsNullOrWhiteSpace(communicationType))
            {
                if (_eventingMethods.Count == 1)
                {
                    eventingMethod = _eventingMethods.First().Value;
                }
                else
                {
                    throw new CommunicationMethodNotFoundException("Multiple communication methods are available.");
                }
            }
            else
            {
                if (!_eventingMethods.TryGetValue(communicationType, out eventingMethod))
                {
                    throw new CommunicationMethodNotFoundException($"Communication method '{communicationType}' is not registered.");
                }
            }

            var servicesSection = _configuration.GetSection("services");
            var serviceSection = servicesSection.GetSection(serviceDefinition.Name);

            IConfiguration publisherConfig;

            var serviceCategory = serviceDefinition.Type == ServiceType.External ? "_external" : "_local";

            publisherConfig = GetConfiguraion(
                _configuration.GetSection("communication"),
                _configuration.GetSection("events").GetSection("communication"),
                servicesSection.GetSection(serviceCategory).GetSection("communication"),
                servicesSection.GetSection(serviceCategory).GetSection("events").GetSection("communication"),
                serviceSection.GetSection("communication"),
                serviceSection.GetSection("events").GetSection("_all").GetSection("communication"),
                serviceSection.GetSection("events").GetSection(eventDefinition.Name).GetSection("communication"));

            var publisher = eventingMethod.CreateEventPublisher(publisherConfig);

            lock (_publisherMap)
            {
                if (_publisherMap.TryGetValue(eventDefinition, out var cachedPublisher))
                {
                    (publisher as IDisposable)?.Dispose();
                    return cachedPublisher;
                }

                _publisherMap.Add(eventDefinition, publisher);
                return publisher;
            }
        }

        private static IConfiguration GetConfiguraion(params IConfiguration[] sections)
        {
            var configBuilder = new ConfigurationBuilder();
            foreach (var section in sections)
                configBuilder.AddConfiguration(section);
            return configBuilder.Build();
        }
    }
}
