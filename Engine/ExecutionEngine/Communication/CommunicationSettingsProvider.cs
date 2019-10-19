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
        private readonly Func<object, object> _valueFactory;

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
        }

        public MethodCommunicationSettings GetMethodSettings(IMethodDefinition methodDefinition) =>
            (MethodCommunicationSettings)_settings.GetOrAdd(methodDefinition, _valueFactory);

        public EventCommunicationSettings GetEventSettings(IEventDefinition eventDefinition) =>
            (EventCommunicationSettings)_settings.GetOrAdd(eventDefinition, _valueFactory);

        private object ComposeSettings(object definition)
        {
            if (definition is IMethodDefinition methodDefinition)
                return ComposeMethodCommunicationSettings(methodDefinition);
            else
                return ComposeEventCommunicationSettings((IEventDefinition)definition);
        }

        private MethodCommunicationSettings ComposeMethodCommunicationSettings(IMethodDefinition definition)
        {
            var isExternal = definition.Service.Type == ServiceType.External;
            var isQuery = definition.IsQuery;

            return new MethodCommunicationSettings
            {
                CommunicationType = GetValue<string>(
                    definition, definition.Service, definition.Service.Model,
                    "communicationType",
                    isQuery ? "communicationType:queries" : "communicationType:commands",
                    isExternal
                    ? (isQuery ? "communicationType:queries:external" : "communicationType:commands:external")
                    : (isQuery ? "communicationType:queries:local" : "communicationType:commands:local"),
                    defaultValue: null),

                Deduplicate = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "deduplicate",
                    isQuery ? "deduplicate:queries" : "deduplicate:commands",
                    isExternal
                    ? (isQuery ? "deduplicate:queries:external" : "deduplicate:commands:external")
                    : (isQuery ? "deduplicate:queries:local" : "deduplicate:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.Deduplicate : CommandsDefaults.Deduplicate),

                Resilient = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "resilient",
                    isQuery ? "resilient:queries" : "resilient:commands",
                    isExternal
                    ? (isQuery ? "resilient:queries:external" : "resilient:commands:external")
                    : (isQuery ? "resilient:queries:local" : "resilient:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.Resilient : CommandsDefaults.Resilient),

                Persistent = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "persistent",
                    isQuery ? "persistent:queries" : "persistent:commands",
                    isExternal
                    ? (isQuery ? "persistent:queries:external" : "persistent:commands:external")
                    : (isQuery ? "persistent:queries:local" : "persistent:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.Persistent : CommandsDefaults.Persistent),

                RoamingState = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "roamingstate",
                    isQuery ? "roamingstate:queries" : "roamingstate:commands",
                    isExternal
                    ? (isQuery ? "roamingstate:queries:external" : "roamingstate:commands:external")
                    : (isQuery ? "roamingstate:queries:local" : "roamingstate:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.RoamingState : CommandsDefaults.RoamingState),

                Transactional = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "transactional",
                    isQuery ? "transactional:queries" : "transactional:commands",
                    isExternal
                    ? (isQuery ? "transactional:queries:external" : "transactional:commands:external")
                    : (isQuery ? "transactional:queries:local" : "transactional:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.Transactional : CommandsDefaults.Transactional),

                RunInPlace = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "runinplace",
                    isQuery ? "runinplace:queries" : "runinplace:commands",
                    isExternal
                    ? (isQuery ? "runinplace:queries:external" : "runinplace:commands:external")
                    : (isQuery ? "runinplace:queries:local" : "runinplace:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.RunInPlace : CommandsDefaults.RunInPlace),

                IgnoreTransaction = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "ignoretransaction",
                    isQuery ? "ignoretransaction:queries" : "ignoretransaction:commands",
                    isExternal
                    ? (isQuery ? "ignoretransaction:queries:external" : "ignoretransaction:commands:external")
                    : (isQuery ? "ignoretransaction:queries:local" : "ignoretransaction:commands:local"),
                    defaultValue: isQuery ? QueriesDefaults.IgnoreTransaction : CommandsDefaults.IgnoreTransaction),
            };
        }

        private EventCommunicationSettings ComposeEventCommunicationSettings(IEventDefinition definition)
        {
            var isExternal = definition.Service.Type == ServiceType.External;

            return new EventCommunicationSettings
            {
                CommunicationType = GetValue<string>(
                    definition, definition.Service, definition.Service.Model,
                    "communicationType",
                    "communicationType:events",
                    isExternal
                    ? "communicationType:events:external"
                    : "communicationType:events:local",
                    defaultValue: null),

                Deduplicate = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "deduplicate",
                    "deduplicate:events",
                    isExternal
                    ? "deduplicate:events:external"
                    : "deduplicate:events:local",
                    defaultValue: EventsDefaults.Deduplicate),

                Resilient = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "resilient",
                    "resilient:events",
                    isExternal
                    ? "resilient:events:external"
                    : "resilient:events:local",
                    defaultValue: EventsDefaults.Resilient),

                IgnoreTransaction = GetValue(
                    definition, definition.Service, definition.Service.Model,
                    "ignoretransaction",
                    "ignoretransaction:events",
                    isExternal
                    ? "ignoretransaction:events:external"
                    : "ignoretransaction:events:local",
                    defaultValue: EventsDefaults.IgnoreTransaction),
            };
        }

        private T GetValue<T>(IPropertyBag topBag, IPropertyBag midBag, IPropertyBag lowBag,
            string propertyName, string categoryPropertyName, string subCategoryPropName, T defaultValue)
        {
            var prop = topBag.FindProperty(propertyName);
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

            prop = lowBag.FindProperty(propertyName);
            if (prop?.Value != null)
                return (T)prop.Value;

            return defaultValue;
        }
    }
}
