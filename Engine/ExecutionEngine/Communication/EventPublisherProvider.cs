using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Resolvers;
using Dasync.ExecutionEngine.Eventing;
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

            var localSettings = _communicationSettingsProvider.GetEventSettings(eventDefinition, external: false);
            var externalSettings = _communicationSettingsProvider.GetEventSettings(eventDefinition, external: true);

            var localEventingMethod = GetEventingMethod(localSettings.CommunicationType);
            var externalEventingMethod = GetEventingMethod(externalSettings.CommunicationType);

            var localPublisher = localEventingMethod.CreateEventPublisher(GetConfiguration(eventDefinition));

            var publisher = localPublisher;

            if (externalEventingMethod.Type != localEventingMethod.Type)
            {
                var externalPublisher = externalEventingMethod.CreateEventPublisher(GetConfiguration(eventDefinition, forceExternal: true));
                publisher = new MulticastEventPublisher(localPublisher, externalPublisher);
            }

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

        private IEventingMethod GetEventingMethod(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                if (_eventingMethods.Count == 1)
                {
                    return _eventingMethods.First().Value;
                }
                else
                {
                    throw new CommunicationMethodNotFoundException("Multiple eventing methods are available.");
                }
            }
            else if (_eventingMethods.TryGetValue(type, out var eventingMethod))
            {
                return eventingMethod;
            }
            else
            {
                throw new CommunicationMethodNotFoundException($"Eventing method '{type}' is not registered.");
            }
        }

        private IConfiguration GetConfiguration(IEventDefinition eventDefinition, bool forceExternal = false)
        {
            var serviceCategory = (forceExternal || eventDefinition.Service.Type == ServiceType.External) ? "_external" : "_local";

            var servicesSection = _configuration.GetSection("services");
            var serviceSection = servicesSection.GetSection(eventDefinition.Service.Name);

            return CombineConfiguraion(
                _configuration.GetSection("communication"),
                _configuration.GetSection("events").GetSection("communication"),
                servicesSection.GetSection(serviceCategory).GetSection("communication"),
                servicesSection.GetSection(serviceCategory).GetSection("events").GetSection("communication"),
                serviceSection.GetSection("communication"),
                serviceSection.GetSection("events").GetSection("_all").GetSection("communication"),
                serviceSection.GetSection("events").GetSection(eventDefinition.Name).GetSection("communication"));
        }

        private static IConfiguration CombineConfiguraion(params IConfiguration[] sections)
        {
            var configBuilder = new ConfigurationBuilder();
            foreach (var section in sections)
                configBuilder.AddConfiguration(section);
            return configBuilder.Build();
        }
    }
}
