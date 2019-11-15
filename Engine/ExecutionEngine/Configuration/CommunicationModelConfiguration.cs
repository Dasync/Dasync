using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.EETypes;
using Dasync.EETypes.Configuration;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.ExecutionEngine.Configuration
{
    public class CommunicationModelConfiguration : ICommunicationModelConfiguration
    {
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;
        private readonly IEventResolver _eventResolver;
        private Dictionary<ConfigurationSectionKey, IConfigurationSection> _configMap;

        public CommunicationModelConfiguration(
            IEnumerable<IConfiguration> safeConfiguration,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver,
            IEventResolver eventResolver)
        {
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;
            _eventResolver = eventResolver;

            var rootSection = safeConfiguration.FirstOrDefault()?.GetSection("dasync");
            _configMap = ReadConfiguration(rootSection);
        }

        private Dictionary<ConfigurationSectionKey, IConfigurationSection> ReadConfiguration(IConfigurationSection rootSection)
        {
            // [base]
            // ------------------------------------------
            // dasync

            // [base+primitives]
            // ------------------------------------------
            // dasync:commands
            // dasync:queries
            // dasync:events

            // [category]
            // ------------------------------------------
            // dasync:services:_local
            // dasync:services:_external

            // [category+primitives]
            // ------------------------------------------
            // dasync:services:_local:commands
            // dasync:services:_local:queries
            // dasync:services:_local:events
            // dasync:services:_external:commands
            // dasync:services:_external:queries
            // dasync:services:_external:events

            // [service]
            // dasync:services:{name}

            // [service+primitives]
            // dasync:services:{name}:commands:_all
            // dasync:services:{name}:queries:_all
            // dasync:services:{name}:events:_all

            // [primitives]
            // dasync:services:{name}:commands:{name}
            // dasync:services:{name}:queries:{name}
            // dasync:services:{name}:events:{name}

            var map = new Dictionary<ConfigurationSectionKey, IConfigurationSection>();

            var baseKey = new ConfigurationSectionKey();
            map[baseKey] = rootSection;

            if (rootSection == null)
                return map;

            AddPrimitiveSections(rootSection, baseKey, map);

            var servicesSection = rootSection.GetSection("services");

            foreach (var serviceSection in servicesSection.GetChildren())
            {
                if (serviceSection.Key.Equals("_local", StringComparison.OrdinalIgnoreCase))
                {
                    var localServicesKey = new ConfigurationSectionKey { ServiceCategory = ServiceCategory.Local };
                    map[localServicesKey] = serviceSection;
                    AddPrimitiveSections(serviceSection, localServicesKey, map);
                }
                else if (serviceSection.Key.Equals("_external", StringComparison.OrdinalIgnoreCase))
                {
                    var externalServicesKey = new ConfigurationSectionKey { ServiceCategory = ServiceCategory.External };
                    map[externalServicesKey] = serviceSection;
                    AddPrimitiveSections(serviceSection, externalServicesKey, map);
                }
                else
                {
                    var serviceName = serviceSection.Key;
                    if (_serviceResolver.TryResolve(new ServiceId { Name = serviceName }, out var serviceReference))
                        serviceName = serviceReference.Definition.Name;

                    var serviceKey = new ConfigurationSectionKey { ServiceName = serviceName };
                    map[serviceKey] = serviceSection;

                    foreach (var subSection in serviceSection.GetChildren())
                    {
                        if (subSection.Key.Equals("commands", StringComparison.OrdinalIgnoreCase))
                        {
                            EnumerateThroughPrimitiveType(subSection, serviceKey, PrimitiveType.Command, serviceReference?.Definition, map);
                        }
                        else if (subSection.Key.Equals("queries", StringComparison.OrdinalIgnoreCase))
                        {
                            EnumerateThroughPrimitiveType(subSection, serviceKey, PrimitiveType.Query, serviceReference?.Definition, map);
                        }
                        else if (subSection.Key.Equals("events", StringComparison.OrdinalIgnoreCase))
                        {
                            EnumerateThroughPrimitiveType(subSection, serviceKey, PrimitiveType.Event, serviceReference?.Definition, map);
                        }
                    }
                }
            }

            return map;
        }

        private void AddPrimitiveSections(
            IConfigurationSection parentSection, ConfigurationSectionKey parentKey,
            Dictionary<ConfigurationSectionKey, IConfigurationSection> map)
        {
            var commandsSection = parentSection.GetSection("commands");
            var commandsSectionKey = parentKey;
            commandsSectionKey.PrimitiveType = PrimitiveType.Command;
            map[commandsSectionKey] = commandsSection.Exists() ? commandsSection : null;

            var queriesSection = parentSection.GetSection("queries");
            var queriesSectionKey = parentKey;
            queriesSectionKey.PrimitiveType = PrimitiveType.Query;
            map[queriesSectionKey] = queriesSection.Exists() ? queriesSection : null;

            var eventsSection = parentSection.GetSection("events");
            var eventsSectionKey = parentKey;
            eventsSectionKey.PrimitiveType = PrimitiveType.Event;
            map[eventsSectionKey] = eventsSection.Exists() ? eventsSection : null;
        }

        private void EnumerateThroughPrimitiveType(
            IConfigurationSection parentSection, ConfigurationSectionKey serviceKey,
            PrimitiveType type, IServiceDefinition serviceDefinition,
            Dictionary<ConfigurationSectionKey, IConfigurationSection> map)
        {
            foreach (var section in parentSection.GetChildren())
            {
                if (section.Key.Equals("_all", StringComparison.OrdinalIgnoreCase))
                {
                    var allPrimitivesKey = serviceKey;
                    allPrimitivesKey.PrimitiveType = type;
                    map[allPrimitivesKey] = section;
                }
                else if (type == PrimitiveType.Command || type == PrimitiveType.Query)
                {
                    var methodType = type;
                    var methodName = section.Key;

                    if (serviceDefinition != null && _methodResolver.TryResolve(serviceDefinition,
                        new MethodId { Name = methodName }, out var methodReference))
                    {
                        methodType = methodReference.Definition.IsQuery ? PrimitiveType.Query : PrimitiveType.Command;
                        methodName = methodReference.Definition.Name;
                    }

                    var methodKey = serviceKey;
                    methodKey.PrimitiveType = methodType;
                    methodKey.PrimitiveName = methodName;

                    map[methodKey] = section;
                }
                else if (type == PrimitiveType.Event)
                {
                    var eventName = section.Key;

                    if (serviceDefinition != null && _eventResolver.TryResolve(serviceDefinition,
                        new EventId { Name = eventName }, out var eventReference))
                    {
                        eventName = eventReference.Definition.Name;
                    }

                    var eventKey = serviceKey;
                    eventKey.PrimitiveType = type;
                    eventKey.PrimitiveName = eventName;

                    map[eventKey] = section;
                }
            }
        }

        public ConfigOverrideLevels GetServiceOverrideLevels(IServiceDefinition serviceDefinition, string sectionName)
        {
            var result = ConfigOverrideLevels.None;

            var serviceCategory = serviceDefinition.Type == ServiceType.External ? ServiceCategory.External : ServiceCategory.Local;

            // [base]
            var key = new ConfigurationSectionKey
            {
                SectionName = sectionName
            };
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Base;

            // [service category]
            key.ServiceCategory = serviceCategory;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.ServiceType;

            // [service]
            key.ServiceCategory = ServiceCategory.Any;
            key.ServiceName = serviceDefinition.Name;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Service;

            return result;
        }

        public IConfiguration GetServiceConfiguration(IServiceDefinition serviceDefinition, string sectionName)
        {
            var sections = new List<IConfigurationSection>(3);

            var serviceCategory = serviceDefinition.Type == ServiceType.External ? ServiceCategory.External : ServiceCategory.Local;

            // [base]
            var key = new ConfigurationSectionKey
            {
                SectionName = sectionName
            };
            if (TryGetSection(key, out var section))
                sections.Add(section);

            // [service category]
            key.ServiceCategory = serviceCategory;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service]
            key.ServiceCategory = ServiceCategory.Any;
            key.ServiceName = serviceDefinition.Name;
            if (TryGetSection(key, out section))
                sections.Add(section);

            return CombineSections(sections);
        }

        public ConfigOverrideLevels GetCommandsOverrideLevels(IServiceDefinition serviceDefinition, string sectionName = null) =>
            GetOverrideLevels(serviceDefinition, PrimitiveType.Command, sectionName, forceExternal: false);

        public ConfigOverrideLevels GetQueriesOverrideLevels(IServiceDefinition serviceDefinition, string sectionName = null) =>
            GetOverrideLevels(serviceDefinition, PrimitiveType.Query, sectionName, forceExternal: false);

        public ConfigOverrideLevels GetEventsOverrideLevels(IServiceDefinition serviceDefinition, string sectionName = null, bool forceExternal = false) =>
            GetOverrideLevels(serviceDefinition, PrimitiveType.Event, sectionName, forceExternal);

        private ConfigOverrideLevels GetOverrideLevels(IServiceDefinition serviceDefinition, PrimitiveType primitiveType, string sectionName, bool forceExternal)
        {
            var result = ConfigOverrideLevels.None;

            var serviceCategory = forceExternal || serviceDefinition.Type == ServiceType.External ? ServiceCategory.External : ServiceCategory.Local;

            // [base]
            var key = new ConfigurationSectionKey
            {
                SectionName = sectionName
            };
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Base;

            // [primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.BasePrimitives;

            // [service category]
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceCategory = serviceCategory;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.ServiceType;

            // [service category + primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.ServiceTypePrimitives;

            // [service]
            key.ServiceCategory = ServiceCategory.Any;
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceName = serviceDefinition.Name;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Service;

            // [service + primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Primitive;

            return result;
        }

        public IConfiguration GetCommandsConfiguration(IServiceDefinition serviceDefinition, string sectionName) =>
            ComposeConfiguration(serviceDefinition, PrimitiveType.Command, sectionName, forceExternal: false);

        public IConfiguration GetQueriesConfiguration(IServiceDefinition serviceDefinition, string sectionName) =>
            ComposeConfiguration(serviceDefinition, PrimitiveType.Query, sectionName, forceExternal: false);

        public IConfiguration GetEventsConfiguration(IServiceDefinition serviceDefinition, string sectionName, bool forceExternal) =>
            ComposeConfiguration(serviceDefinition, PrimitiveType.Event, sectionName, forceExternal);

        private IConfiguration ComposeConfiguration(IServiceDefinition serviceDefinition, PrimitiveType primitiveType, string sectionName, bool forceExternal)
        {
            var sections = new List<IConfigurationSection>(6);

            var serviceCategory = forceExternal || serviceDefinition.Type == ServiceType.External ? ServiceCategory.External : ServiceCategory.Local;

            // [base]
            var key = new ConfigurationSectionKey
            {
                SectionName = sectionName
            };
            if (TryGetSection(key, out var section))
                sections.Add(section);

            // [primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service category]
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceCategory = serviceCategory;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service category + primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service]
            key.ServiceCategory = ServiceCategory.Any;
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceName = serviceDefinition.Name;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service + primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out section))
                sections.Add(section);

            return CombineSections(sections);
        }

        public ConfigOverrideLevels GetMethodOverrideLevels(IMethodDefinition methodDefinition, string sectionName = null)
        {
            var result = ConfigOverrideLevels.None;

            var primitiveType = methodDefinition.IsQuery ? PrimitiveType.Query : PrimitiveType.Command;
            var serviceCategory = methodDefinition.Service.Type == ServiceType.External ? ServiceCategory.External : ServiceCategory.Local;

            // [base]
            var key = new ConfigurationSectionKey
            {
                SectionName = sectionName
            };
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Base;

            // [primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.BasePrimitives;

            // [service category]
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceCategory = serviceCategory;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.ServiceType;

            // [service category + primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.ServiceTypePrimitives;

            // [service]
            key.ServiceCategory = ServiceCategory.Any;
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceName = methodDefinition.Service.Name;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Service;

            // [service + primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.ServicePrimitives;

            // [primitive]
            key.PrimitiveName = methodDefinition.Name;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Primitive;

            return result;
        }

        public IConfiguration GetMethodConfiguration(IMethodDefinition methodDefinition, string sectionName)
        {
            var sections = new List<IConfigurationSection>(7);

            var primitiveType = methodDefinition.IsQuery ? PrimitiveType.Query : PrimitiveType.Command;
            var serviceCategory = methodDefinition.Service.Type == ServiceType.External ? ServiceCategory.External : ServiceCategory.Local;

            // [base]
            var key = new ConfigurationSectionKey
            {
                SectionName = sectionName
            };
            if (TryGetSection(key, out var section))
                sections.Add(section);

            // [primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service category]
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceCategory = serviceCategory;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service category + primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service]
            key.ServiceCategory = ServiceCategory.Any;
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceName = methodDefinition.Service.Name;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service + primitive type]
            key.PrimitiveType = primitiveType;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [primitive]
            key.PrimitiveName = methodDefinition.Name;
            if (TryGetSection(key, out section))
                sections.Add(section);

            return CombineSections(sections);
        }

        public ConfigOverrideLevels GetEventOverrideLevels(IEventDefinition eventDefinition, string sectionName = null, bool forceExternal = false)
        {
            var result = ConfigOverrideLevels.None;

            var serviceCategory = forceExternal || eventDefinition.Service.Type == ServiceType.External ? ServiceCategory.External : ServiceCategory.Local;

            // [base]
            var key = new ConfigurationSectionKey
            {
                SectionName = sectionName
            };
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Base;

            // [primitive type]
            key.PrimitiveType = PrimitiveType.Event;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.BasePrimitives;

            // [service category]
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceCategory = serviceCategory;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.ServiceType;

            // [service category + primitive type]
            key.PrimitiveType = PrimitiveType.Event;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.ServiceTypePrimitives;

            // [service]
            key.ServiceCategory = ServiceCategory.Any;
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceName = eventDefinition.Service.Name;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Service;

            // [service + primitive type]
            key.PrimitiveType = PrimitiveType.Event;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.ServicePrimitives;

            // [primitive]
            key.PrimitiveName = eventDefinition.Name;
            if (TryGetSection(key, out _))
                result |= ConfigOverrideLevels.Primitive;

            return result;
        }

        public IConfiguration GetEventConfiguration(IEventDefinition eventDefinition, string sectionName, bool forceExternal)
        {
            var sections = new List<IConfigurationSection>(7);

            var serviceCategory = forceExternal || eventDefinition.Service.Type == ServiceType.External ? ServiceCategory.External : ServiceCategory.Local;

            // [base]
            var key = new ConfigurationSectionKey
            {
                SectionName = sectionName
            };
            if (TryGetSection(key, out var section))
                sections.Add(section);

            // [primitive type]
            key.PrimitiveType = PrimitiveType.Event;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service category]
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceCategory = serviceCategory;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service category + primitive type]
            key.PrimitiveType = PrimitiveType.Event;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service]
            key.ServiceCategory = ServiceCategory.Any;
            key.PrimitiveType = PrimitiveType.Any;
            key.ServiceName = eventDefinition.Service.Name;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [service + primitive type]
            key.PrimitiveType = PrimitiveType.Event;
            if (TryGetSection(key, out section))
                sections.Add(section);

            // [primitive]
            key.PrimitiveName = eventDefinition.Name;
            if (TryGetSection(key, out section))
                sections.Add(section);

            return CombineSections(sections);
        }

        private static IConfiguration CombineSections(IEnumerable<IConfigurationSection> sections)
        {
            var configBuilder = new ConfigurationBuilder();
            foreach (var section in sections)
                configBuilder.AddConfiguration(section);
            return configBuilder.Build();
        }

        private bool TryGetSection(ConfigurationSectionKey key, out IConfigurationSection section)
        {
            if (_configMap.TryGetValue(key, out section))
                return section != null;

            var sectionName = key.SectionName;
            if (sectionName == null)
            {
                section = null;
                return false;
            }

            key.SectionName = null;
            if (!_configMap.TryGetValue(key, out var baseSection) || baseSection == null)
            {
                section = null;
                return false;
            }

            key.SectionName = sectionName;

            var targetSection = baseSection.GetSection(sectionName);
            if (!targetSection.Exists())
            {
                _configMap.Add(key, null);
                section = null;
                return false;
            }
            else
            {
                _configMap.Add(key, targetSection);
                section = targetSection;
                return true;
            }
        }
    }
}
