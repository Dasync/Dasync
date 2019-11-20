using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Configuration;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Eventing;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.ExecutionEngine.Startup
{
    public interface ICommunicationListener
    {
        Task StartAsync(CancellationToken ct);

        Task StopAsync(CancellationToken ct);
    }

    public class CommunicationListener : ICommunicationListener
    {
        private readonly ICommunicationModel _communicationModel;
        private readonly ICommunicationModelConfiguration _communicationModelConfiguration;
        private readonly IEventSubscriber _eventSubscriber;
        private readonly IEventIdProvider _eventIdProvider;
        private readonly IServiceResolver _serviceResolver;
        private readonly Dictionary<string, IMessageListeningMethod> _listeningMethods;

        private const string CommunicationSectionName = "communication";

        public CommunicationListener(
            ICommunicationModel communicationModel,
            ICommunicationModelConfiguration communicationModelConfiguration,
            IEventSubscriber eventSubscriber,
            IEventIdProvider eventIdProvider,
            IServiceResolver serviceResolver,
            IEnumerable<IMessageListeningMethod> listeningMethods)
        {
            _communicationModel = communicationModel;
            _communicationModelConfiguration = communicationModelConfiguration;
            _eventSubscriber = eventSubscriber;
            _eventIdProvider = eventIdProvider;
            _serviceResolver = serviceResolver;

            _listeningMethods = listeningMethods.ToDictionary(m => m.Type, m => m, StringComparer.OrdinalIgnoreCase);
        }

        public async Task StartAsync(CancellationToken ct)
        {
            foreach (var serviceDefinition in _communicationModel.Services)
            {
                if (serviceDefinition.Type == ServiceType.External)
                {
                    var serviceConfig = _communicationModelConfiguration.GetEventsConfiguration(serviceDefinition, CommunicationSectionName);
                    var serviceCommunicationType = GetCommunicationType(serviceConfig);

                    var serviceId = new ServiceId { Name = serviceDefinition.Name };

                    var anySubscriberToAnyEvent = false;

                    var eventOverridesMap = new Dictionary<string, Dictionary<IEventDefinition, IConfiguration>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var eventDefinition in serviceDefinition.Events)
                    {
                        var eventId = _eventIdProvider.GetId(eventDefinition.EventInfo);
                        var eventDesc = new EventDescriptor { Service = serviceId, Event = eventId };

                        var subscribers = _eventSubscriber.GetSubscribers(eventDesc).ToList();

                        if (subscribers.Count == 0)
                            continue;

                        var anySubscriberToThisEvent = false;

                        foreach (var subscriber in subscribers)
                        {
                            if (_serviceResolver.TryResolve(subscriber.Service, out _))
                            {
                                anySubscriberToThisEvent = true;
                                anySubscriberToAnyEvent = true;
                                break;
                            }
                        }

                        if (!anySubscriberToThisEvent)
                            continue;

                        if (_communicationModelConfiguration
                            .GetEventOverrideLevels(eventDefinition, CommunicationSectionName)
                            .HasFlag(ConfigOverrideLevels.Primitive))
                        {
                            var eventConfig = _communicationModelConfiguration.GetEventConfiguration(eventDefinition);
                            var eventCommunicationType = GetCommunicationType(eventConfig);

                            if (!eventOverridesMap.TryGetValue(eventCommunicationType, out var configMap))
                            {
                                configMap = new Dictionary<IEventDefinition, IConfiguration>();
                                eventOverridesMap.Add(eventCommunicationType, configMap);
                            }

                            configMap.Add(eventDefinition, eventConfig);
                        }
                    }

                    if (anySubscriberToAnyEvent)
                    {
                        if (!eventOverridesMap.TryGetValue(serviceCommunicationType, out var configMap))
                            configMap = new Dictionary<IEventDefinition, IConfiguration>();
                        await StartListeningEventsAsync(serviceCommunicationType, serviceDefinition, serviceConfig, configMap, ct);

                        var extraCommTypes = eventOverridesMap.Keys.Where(c => !string.Equals(c, serviceCommunicationType, StringComparison.OrdinalIgnoreCase));
                        foreach (var extraCommType in extraCommTypes)
                        {
                            configMap = eventOverridesMap[extraCommType];
                            await StartListeningEventsAsync(extraCommType, serviceDefinition, serviceConfig, configMap, ct);
                        }
                    }
                }
                else
                {
                    var methodOverridesMap = new Dictionary<string, Dictionary<IMethodDefinition, IConfiguration>>(StringComparer.OrdinalIgnoreCase);
                    var eventOverridesMap = new Dictionary<string, Dictionary<IEventDefinition, IConfiguration>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var methodDefinition in serviceDefinition.Methods)
                    {
                        if (_communicationModelConfiguration
                            .GetMethodOverrideLevels(methodDefinition, CommunicationSectionName)
                            .HasFlag(ConfigOverrideLevels.Primitive))
                        {
                            var methodConfig = _communicationModelConfiguration.GetMethodConfiguration(methodDefinition);
                            var methodCommunicationType = GetCommunicationType(methodConfig);

                            if (!methodOverridesMap.TryGetValue(methodCommunicationType, out var configMap))
                            {
                                configMap = new Dictionary<IMethodDefinition, IConfiguration>();
                                methodOverridesMap.Add(methodCommunicationType, configMap);
                            }

                            configMap.Add(methodDefinition, methodConfig);
                        }
                    }

                    foreach (var eventDefinition in serviceDefinition.Events)
                    {
                        if (_communicationModelConfiguration
                            .GetEventsOverrideLevels(serviceDefinition, CommunicationSectionName)
                            .HasFlag(ConfigOverrideLevels.Primitive))
                        {
                            var eventConfig = _communicationModelConfiguration.GetEventConfiguration(eventDefinition);
                            var eventCommunicationType = GetCommunicationType(eventConfig);

                            if (!eventOverridesMap.TryGetValue(eventCommunicationType, out var configMap))
                            {
                                configMap = new Dictionary<IEventDefinition, IConfiguration>();
                                eventOverridesMap.Add(eventCommunicationType, configMap);
                            }

                            configMap.Add(eventDefinition, eventConfig);
                        }
                    }

                    var hasQueriesOverride = (_communicationModelConfiguration
                            .GetQueriesOverrideLevels(serviceDefinition, CommunicationSectionName)
                             & (ConfigOverrideLevels.ServiceTypePrimitives | ConfigOverrideLevels.ServicePrimitives)) != default;

                    var hasCommandsOverride = (_communicationModelConfiguration
                            .GetCommandsOverrideLevels(serviceDefinition, CommunicationSectionName)
                            & (ConfigOverrideLevels.ServiceTypePrimitives | ConfigOverrideLevels.ServicePrimitives)) != default;

                    if (hasQueriesOverride || hasCommandsOverride)
                    {
                        var allQueriesConfig = _communicationModelConfiguration.GetQueriesConfiguration(serviceDefinition, CommunicationSectionName);
                        var allQueriesCommunicationType = GetCommunicationType(allQueriesConfig);

                        if (!methodOverridesMap.TryGetValue(allQueriesCommunicationType, out var methodConfigMap))
                            methodConfigMap = new Dictionary<IMethodDefinition, IConfiguration>();
                        await StartHadlingMethodsAsync(allQueriesCommunicationType, serviceDefinition, allQueriesConfig, methodConfigMap, ct);

                        var allCommandsConfig = _communicationModelConfiguration.GetCommandsConfiguration(serviceDefinition, CommunicationSectionName);
                        var allCommandsCommunicationType = GetCommunicationType(allCommandsConfig);

                        if (!methodOverridesMap.TryGetValue(allCommandsCommunicationType, out methodConfigMap))
                            methodConfigMap = new Dictionary<IMethodDefinition, IConfiguration>();
                        await StartHadlingMethodsAsync(allCommandsCommunicationType, serviceDefinition, allCommandsConfig, methodConfigMap, ct);

                        var methodsExtraCommTypes = methodOverridesMap.Keys.Where(c =>
                            !string.Equals(c, allQueriesCommunicationType, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(c, allCommandsCommunicationType, StringComparison.OrdinalIgnoreCase));
                        foreach (var methodExtraCommType in methodsExtraCommTypes)
                        {
                            methodConfigMap = methodOverridesMap[methodExtraCommType];

                            var queryConfigMap = new Dictionary<IMethodDefinition, IConfiguration>();
                            var commandConfigMap = new Dictionary<IMethodDefinition, IConfiguration>();
                            foreach (var pair in methodConfigMap)
                            {
                                if (pair.Key.IsQuery)
                                    queryConfigMap.Add(pair.Key, pair.Value);
                                else
                                    commandConfigMap.Add(pair.Key, pair.Value);
                            }

                            if (queryConfigMap.Count > 0)
                                await StartHadlingMethodsAsync(methodExtraCommType, serviceDefinition, allQueriesConfig, queryConfigMap, ct);
                            if (commandConfigMap.Count > 0)
                                await StartHadlingMethodsAsync(methodExtraCommType, serviceDefinition, allCommandsConfig, commandConfigMap, ct);
                        }
                    }
                    else
                    {
                        var serviceConfig = _communicationModelConfiguration.GetServiceConfiguration(serviceDefinition, CommunicationSectionName);
                        var serviceCommunicationType = GetCommunicationType(serviceConfig);

                        if (!methodOverridesMap.TryGetValue(serviceCommunicationType, out var methodConfigMap))
                            methodConfigMap = new Dictionary<IMethodDefinition, IConfiguration>();
                        await StartHadlingMethodsAsync(serviceCommunicationType, serviceDefinition, serviceConfig, methodConfigMap, ct);

                        var methodsExtraCommTypes = methodOverridesMap.Keys.Where(c => !string.Equals(c, serviceCommunicationType, StringComparison.OrdinalIgnoreCase));
                        foreach (var methodExtraCommType in methodsExtraCommTypes)
                        {
                            methodConfigMap = methodOverridesMap[methodExtraCommType];
                            await StartHadlingMethodsAsync(methodExtraCommType, serviceDefinition, serviceConfig, methodConfigMap, ct);
                        }
                    }

                    var allEventsConfig = _communicationModelConfiguration.GetEventsConfiguration(serviceDefinition, CommunicationSectionName);
                    var allEventsCommunicationType = GetCommunicationType(allEventsConfig);

                    if (!eventOverridesMap.TryGetValue(allEventsCommunicationType, out var eventConfigMap))
                        eventConfigMap = new Dictionary<IEventDefinition, IConfiguration>();
                    await StartListeningEventsAsync(allEventsCommunicationType, serviceDefinition, allEventsConfig, eventConfigMap, ct);

                    var eventsExtraCommTypes = eventOverridesMap.Keys.Where(c => !string.Equals(c, allEventsCommunicationType, StringComparison.OrdinalIgnoreCase));
                    foreach (var eventExtraCommType in eventsExtraCommTypes)
                    {
                        eventConfigMap = eventOverridesMap[eventExtraCommType];
                        await StartListeningEventsAsync(eventExtraCommType, serviceDefinition, allEventsConfig, eventConfigMap, ct);
                    }
                }
            }
        }

        public async Task StopAsync(CancellationToken ct)
        {

        }


        private static string GetCommunicationType(IConfiguration config) => config.GetSection("type").Value ?? "";

        private IMessageListeningMethod GetListeningMethod(string type)
        {
            if (string.IsNullOrWhiteSpace(type) && _listeningMethods.Count > 0)
            {
                if (_listeningMethods.Count == 1)
                {
                    return _listeningMethods.First().Value;
                }
                else
                {
                    throw new CommunicationMethodNotFoundException("Multiple message listening methods are available.");
                }
            }
            else
            {
                if (_listeningMethods.TryGetValue(type, out var method))
                {
                    return method;
                }
                else
                {
                    throw new CommunicationMethodNotFoundException($"Message listening method '{type}' is not registered.");
                }
            }
        }

        private async Task StartHadlingMethodsAsync(
            string communicationType,
            IServiceDefinition serviceDefinition,
            IConfiguration configuration,
            Dictionary<IMethodDefinition, IConfiguration> methodConfigMap,
            CancellationToken ct)
        {
            var method = GetListeningMethod(communicationType);
            var listeners = await method.StartListeningAsync(configuration, serviceDefinition, methodConfigMap, ct);
            // TODO: keep referene to the listerners
        }

        private async Task StartListeningEventsAsync(
            string communicationType,
            IServiceDefinition serviceDefinition,
            IConfiguration configuration,
            Dictionary<IEventDefinition, IConfiguration> eventConfigMap,
            CancellationToken ct)
        {
            var method = GetListeningMethod(communicationType);
            var listeners = await method.StartListeningAsync(configuration, serviceDefinition, eventConfigMap, ct);
            // TODO: keep referene to the listerner
        }
    }
}
