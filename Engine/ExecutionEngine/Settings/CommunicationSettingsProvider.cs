using System;
using System.Collections.Concurrent;
using Dasync.EETypes.Communication;
using Dasync.Modeling;

namespace Dasync.ExecutionEngine.Communication
{
    public class CommunicationSettingsProvider : ICommunicationSettingsProvider
    {
        private readonly ConcurrentDictionary<object, object> _settings =
            new ConcurrentDictionary<object, object>();
        private readonly ConcurrentDictionary<IEventDefinition, EventCommunicationSettings> _externalEventSettings =
            new ConcurrentDictionary<IEventDefinition, EventCommunicationSettings>();
        private readonly Func<object, object> _valueFactory;
        private readonly Func<IEventDefinition, EventCommunicationSettings> _externalEventValueFactory;

        private static readonly MethodCommunicationSettings QueriesDefaults =
            new MethodCommunicationSettings
            {
                RunInPlace = true,
                IgnoreTransaction = true,
            };

        private static readonly MethodCommunicationSettings CommandsDefaults =
            new MethodCommunicationSettings
            {
                Deduplicate = true,
                Resilient = true,
                Persistent = true,
                Transactional = true,
            };

        private static readonly EventCommunicationSettings EventsDefaults =
            new EventCommunicationSettings
            {
                Deduplicate = true,
                Resilient = true
            };

        public CommunicationSettingsProvider()
        {
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
            var model = serviceDefinition.Model;
            var isExternal = serviceDefinition.Type == ServiceType.External;
            var isQuery = methodDefinition?.IsQuery == true;

            return new MethodCommunicationSettings
            {
                CommunicationType = GetValue<string>(
                    methodDefinition, serviceDefinition, model,
                    "communicationType",
                    isExternal ? "communicationType:external" : "communicationType:local",
                    isQuery ? "communicationType:queries" : "communicationType:commands",
                    isExternal
                    ? (isQuery ? "communicationType:queries:external" : "communicationType:commands:external")
                    : (isQuery ? "communicationType:queries:local" : "communicationType:commands:local"),
                    defaultValue: null),

                PersistenceType = GetValue<string>(
                    methodDefinition, serviceDefinition, model,
                    "persistenceType",
                    isExternal ? "persistenceType:external" : "persistenceType:local",
                    isQuery ? "persistenceType:queries" : "persistenceType:commands",
                    isExternal
                    ? (isQuery ? "persistenceType:queries:external" : "persistenceType:commands:external")
                    : (isQuery ? "persistenceType:queries:local" : "persistenceType:commands:local"),
                    defaultValue: null),

                Deduplicate = GetValue(
                    methodDefinition, serviceDefinition, model,
                    "deduplicate",
                    isExternal ? "deduplicate:external" : "deduplicate:local",
                    isQuery ? "deduplicate:queries" : "deduplicate:commands",
                    isExternal
                    ? (isQuery ? "deduplicate:queries:external" : "deduplicate:commands:external")
                    : (isQuery ? "deduplicate:queries:local" : "deduplicate:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.Deduplicate : CommandsDefaults.Deduplicate),

                Resilient = GetValue(
                    methodDefinition, serviceDefinition, model,
                    "resilient",
                    isExternal ? "resilient:external" : "resilient:local",
                    isQuery ? "resilient:queries" : "resilient:commands",
                    isExternal
                    ? (isQuery ? "resilient:queries:external" : "resilient:commands:external")
                    : (isQuery ? "resilient:queries:local" : "resilient:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.Resilient : CommandsDefaults.Resilient),

                Persistent = GetValue(
                    methodDefinition, serviceDefinition, model,
                    "persistent",
                    isExternal ? "persistent:external" : "persistent:local",
                    isQuery ? "persistent:queries" : "persistent:commands",
                    isExternal
                    ? (isQuery ? "persistent:queries:external" : "persistent:commands:external")
                    : (isQuery ? "persistent:queries:local" : "persistent:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.Persistent : CommandsDefaults.Persistent),

                RoamingState = GetValue(
                    methodDefinition, serviceDefinition, model,
                    "roamingstate",
                    isExternal ? "roamingstate:external" : "roamingstate:local",
                    isQuery ? "roamingstate:queries" : "roamingstate:commands",
                    isExternal
                    ? (isQuery ? "roamingstate:queries:external" : "roamingstate:commands:external")
                    : (isQuery ? "roamingstate:queries:local" : "roamingstate:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.RoamingState : CommandsDefaults.RoamingState),

                Transactional = GetValue(
                    methodDefinition, serviceDefinition, model,
                    "transactional",
                    isExternal ? "transactional:external" : "transactional:local",
                    isQuery ? "transactional:queries" : "transactional:commands",
                    isExternal
                    ? (isQuery ? "transactional:queries:external" : "transactional:commands:external")
                    : (isQuery ? "transactional:queries:local" : "transactional:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.Transactional : CommandsDefaults.Transactional),

                RunInPlace = GetValue(
                    methodDefinition, serviceDefinition, model,
                    "runinplace",
                    isExternal ? "runinplace:external" : "runinplace:local",
                    isQuery ? "runinplace:queries" : "runinplace:commands",
                    isExternal
                    ? (isQuery ? "runinplace:queries:external" : "runinplace:commands:external")
                    : (isQuery ? "runinplace:queries:local" : "runinplace:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.RunInPlace : CommandsDefaults.RunInPlace),

                IgnoreTransaction = GetValue(
                    methodDefinition, serviceDefinition, model,
                    "ignoretransaction",
                    isExternal ? "ignoretransaction:external" : "ignoretransaction:local",
                    isQuery ? "ignoretransaction:queries" : "ignoretransaction:commands",
                    isExternal
                    ? (isQuery ? "ignoretransaction:queries:external" : "ignoretransaction:commands:external")
                    : (isQuery ? "ignoretransaction:queries:local" : "ignoretransaction:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.IgnoreTransaction : CommandsDefaults.IgnoreTransaction),
            };
        }

        private EventCommunicationSettings ComposeEventCommunicationSettings(IEventDefinition definition, bool forceExternal = false)
        {
            var isExternal = forceExternal || definition.Service.Type == ServiceType.External;

            return new EventCommunicationSettings
            {
                CommunicationType = GetValue<string>(
                    definition, definition.Service, definition.Service.Model,
                    "communicationType",
                    isExternal ? "communicationType:external" : "communicationType:local",
                    "communicationType:events",
                    isExternal ? "communicationType:events:external" : "communicationType:events:local",
                    defaultValue: null),

                Deduplicate = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "deduplicate",
                    isExternal ? "deduplicate:external" : "deduplicate:local",
                    "deduplicate:events",
                    isExternal ? "deduplicate:events:external" : "deduplicate:events:local",
                    defaultValue: EventsDefaults.Deduplicate),

                Resilient = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "resilient",
                    isExternal ? "resilient:external" : "resilient:local",
                    "resilient:events",
                    isExternal ? "resilient:events:external" : "resilient:events:local",
                    defaultValue: EventsDefaults.Resilient),

                IgnoreTransaction = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "ignoretransaction",
                    isExternal ? "ignoretransaction:external" : "ignoretransaction:local",
                    "ignoretransaction:events",
                    isExternal ? "ignoretransaction:events:external" : "ignoretransaction:events:local",
                    defaultValue: EventsDefaults.IgnoreTransaction),
            };
        }

        private T GetValue<T>(IPropertyBag topBag, IPropertyBag midBag, IPropertyBag lowBag,
            string propertyName, string lowBagPropName, string categoryPropertyName, string subCategoryPropName, T defaultValue)
        {
            var prop = topBag?.FindProperty(propertyName);
            if (prop?.Value != null)
                return (T)prop.Value;

            if (subCategoryPropName != null)
            {
                prop = midBag.FindProperty(subCategoryPropName);
                if (prop?.Value != null)
                    return (T)prop.Value;
            }

            if (categoryPropertyName != null)
            {
                prop = midBag.FindProperty(categoryPropertyName);
                if (prop?.Value != null)
                    return (T)prop.Value;
            }

            prop = midBag.FindProperty(propertyName);
            if (prop?.Value != null)
                return (T)prop.Value;

            if (subCategoryPropName != null)
            {
                prop = lowBag.FindProperty(subCategoryPropName);
                if (prop?.Value != null)
                    return (T)prop.Value;
            }

            if (categoryPropertyName != null)
            {
                prop = lowBag.FindProperty(categoryPropertyName);
                if (prop?.Value != null)
                    return (T)prop.Value;
            }

            prop = lowBag.FindProperty(lowBagPropName);
            if (prop?.Value != null)
                return (T)prop.Value;

            prop = lowBag.FindProperty(propertyName);
            if (prop?.Value != null)
                return (T)prop.Value;

            return defaultValue;
        }
    }
}
