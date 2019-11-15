using System;
using System.Collections.Concurrent;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Configuration;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.ExecutionEngine.Communication
{
    public class CommunicationSettingsProvider : ICommunicationSettingsProvider
    {
        private const string CommunicationSectionName = "communication";
        private const string PersistenceSectionName = "persistence";

        private readonly ICommunicationModelConfiguration _communicationModelConfiguration;
        private readonly ConcurrentDictionary<object, object> _settings =
            new ConcurrentDictionary<object, object>();
        private readonly ConcurrentDictionary<IEventDefinition, EventCommunicationSettings> _externalEventSettings =
            new ConcurrentDictionary<IEventDefinition, EventCommunicationSettings>();
        private readonly Func<object, object> _valueFactory;
        private readonly Func<IEventDefinition, EventCommunicationSettings> _externalEventValueFactory;

        public CommunicationSettingsProvider(ICommunicationModelConfiguration communicationModelConfiguration)
        {
            _communicationModelConfiguration = communicationModelConfiguration;
            _valueFactory = ComposeSettings;
            _externalEventValueFactory = ComposeSettingsForExternalEventing;
        }

        public MethodCommunicationSettings GetServiceMethodSettings(IServiceDefinition serviceDefinition) =>
            (MethodCommunicationSettings)_settings.GetOrAdd(serviceDefinition, _valueFactory);

        public MethodCommunicationSettings GetMethodSettings(IMethodDefinition methodDefinition) =>
            (MethodCommunicationSettings)_settings.GetOrAdd(methodDefinition, _valueFactory);

        public EventCommunicationSettings GetEventSettings(IEventDefinition eventDefinition, bool external) =>
            external
            ? _externalEventSettings.GetOrAdd(eventDefinition, _externalEventValueFactory)
            : (EventCommunicationSettings)_settings.GetOrAdd(eventDefinition, _valueFactory);

        private object ComposeSettings(object definition)
        {
            if (definition is IServiceDefinition serviceDefinition)
                return ComposeMethodCommunicationSettings(serviceDefinition, null);
            else if (definition is IMethodDefinition methodDefinition)
                return ComposeMethodCommunicationSettings(methodDefinition.Service, methodDefinition);
            else
                return ComposeEventCommunicationSettings((IEventDefinition)definition);
        }

        private EventCommunicationSettings ComposeSettingsForExternalEventing(IEventDefinition definition)
        {
            return ComposeEventCommunicationSettings(definition, forceExternal: true);
        }

        private MethodCommunicationSettings ComposeMethodCommunicationSettings(IServiceDefinition serviceDefinition, IMethodDefinition methodDefinition)
        {
            var settings = new MethodCommunicationSettings();

            if (methodDefinition != null)
            {
                if (methodDefinition.IsQuery)
                {
                    settings.RunInPlace = true;
                    settings.IgnoreTransaction = true;
                }
                else
                {
                    settings.Deduplicate = true;
                    settings.Resilient = true;
                    settings.Persistent = true;
                    settings.Transactional = true;
                }

                var configuration = _communicationModelConfiguration.GetMethodConfiguration(methodDefinition);

                configuration.Bind(settings);

                settings.CommunicationType =
                    _communicationModelConfiguration
                    .GetMethodConfiguration(methodDefinition, CommunicationSectionName)
                    .GetSection("type").Value;

                settings.CommunicationType =
                    _communicationModelConfiguration
                    .GetMethodConfiguration(methodDefinition, PersistenceSectionName)
                    .GetSection("type").Value;
            }
            else
            {
                var configuration = _communicationModelConfiguration.GetServiceConfiguration(serviceDefinition);

                configuration.Bind(settings);

                settings.CommunicationType =
                    _communicationModelConfiguration
                    .GetServiceConfiguration(serviceDefinition, CommunicationSectionName)
                    .GetSection("type").Value;

                settings.PersistenceType =
                    _communicationModelConfiguration
                    .GetServiceConfiguration(serviceDefinition, PersistenceSectionName)
                    .GetSection("type").Value;
            }

            return settings;
        }

        private EventCommunicationSettings ComposeEventCommunicationSettings(IEventDefinition definition, bool forceExternal = false)
        {
            var settings = new EventCommunicationSettings
            {
                Deduplicate = true,
                Resilient = true
            };

            var configuration = _communicationModelConfiguration.GetEventConfiguration(definition, null, forceExternal);

            configuration.Bind(settings);

            settings.CommunicationType =
                _communicationModelConfiguration
                .GetEventConfiguration(definition, CommunicationSectionName, forceExternal)
                .GetSection("type").Value;

            return settings;
        }
    }
}
