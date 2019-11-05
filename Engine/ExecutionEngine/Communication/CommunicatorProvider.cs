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
    public class CommunicatorProvider : ICommunicatorProvider
    {
        private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
        private readonly IConfiguration _configuration;
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;
        private readonly IExternalCommunicationModel _externalCommunicationModel;
        private readonly Dictionary<string, ICommunicationMethod> _communicationMethods;
        private readonly Dictionary<object, ICommunicator> _communicatorMap = new Dictionary<object, ICommunicator>();

        public CommunicatorProvider(
            ICommunicationSettingsProvider communicationSettingsProvider,
            IEnumerable<IConfiguration> safeConfiguration,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver,
            IExternalCommunicationModel externalCommunicationModel,
            IEnumerable<ICommunicationMethod> communicationMethods)
        {
            _communicationSettingsProvider = communicationSettingsProvider;
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;
            _externalCommunicationModel = externalCommunicationModel;

            _configuration = safeConfiguration.FirstOrDefault()?.GetSection("dasync")
                ?? (IConfiguration)new ConfigurationRoot(Array.Empty<IConfigurationProvider>());

            _communicationMethods = communicationMethods.ToDictionary(m => m.Type, m => m, StringComparer.OrdinalIgnoreCase);
        }

        public ICommunicator GetCommunicator(ServiceId serviceId, MethodId methodId, bool assumeExternal = false)
        {
            if (_communicationMethods.Count == 0)
                throw new CommunicationMethodNotFoundException("There are no communication methods registered.");

            IServiceDefinition serviceDefinition;
            IMethodDefinition methodDefinition;

            if (_serviceResolver.TryResolve(serviceId, out var serviceRef))
            {
                serviceDefinition = serviceRef.Definition;

                // NOTE: system services are not unique within a multi-service ecosystem, thus must
                // use the configuration of the calling (proxy) service without any specific method.
                // Otherwise, a continuation can be sent to a wrong instance of a system service.
                if (serviceDefinition.Type == ServiceType.System && !string.IsNullOrEmpty(serviceId.Proxy))
                {
                    return GetCommunicator(new ServiceId { Name = serviceId.Proxy }, null, assumeExternal);
                }

                methodDefinition = methodId == null ? null : _methodResolver.Resolve(serviceDefinition, methodId).Definition;
            }
            else if (assumeExternal)
            {
                var externalServiceDefinition = _externalCommunicationModel.GetOrAddService(serviceId);
                var externalMethodDefinition = methodId == null ? null : externalServiceDefinition.GetOrAddMethod(methodId);
                serviceDefinition = externalServiceDefinition;
                methodDefinition = externalMethodDefinition;
            }
            else
            {
                throw new ServiceResolveException(serviceId);
            }

            var key = (object)methodDefinition ?? serviceDefinition;
            lock (_communicatorMap)
            {
                if (_communicatorMap.TryGetValue(key, out var cachedCommunicator))
                    return cachedCommunicator;
            }

            MethodCommunicationSettings methodCommunicationSettings =
                methodDefinition == null
                ? _communicationSettingsProvider.GetServiceMethodSettings(serviceDefinition)
                : _communicationSettingsProvider.GetMethodSettings(methodDefinition);

            var communicationType = methodCommunicationSettings.CommunicationType;

            ICommunicationMethod communicationMethod;
            if (string.IsNullOrWhiteSpace(communicationType))
            {
                if (_communicationMethods.Count == 1)
                {
                    communicationMethod = _communicationMethods.First().Value;
                }
                else
                {
                    throw new CommunicationMethodNotFoundException("Multiple communication methods are available.");
                }
            }
            else
            {
                if (!_communicationMethods.TryGetValue(communicationType, out communicationMethod))
                {
                    throw new CommunicationMethodNotFoundException($"Communication method '{communicationType}' is not registered.");
                }
            }

            var servicesSection = _configuration.GetSection("services");
            var serviceSection = servicesSection.GetSection(serviceDefinition.Name);

            IConfiguration communicatorConfig;

            var serviceCategory = serviceDefinition.Type == ServiceType.External ? "_external" : "_local";

            if (methodDefinition == null)
            {
                communicatorConfig = GetConfiguraion(
                    _configuration.GetSection("communication"),
                    servicesSection.GetSection(serviceCategory).GetSection("communication"),
                    serviceSection.GetSection("communication"));
            }
            else
            {
                var methodCategory = methodDefinition.IsQuery ? "queries" : "commands";

                communicatorConfig = GetConfiguraion(
                    _configuration.GetSection("communication"),
                    _configuration.GetSection(methodCategory).GetSection("communication"),
                    servicesSection.GetSection(serviceCategory).GetSection("communication"),
                    servicesSection.GetSection(serviceCategory).GetSection(methodCategory).GetSection("communication"),
                    serviceSection.GetSection("communication"),
                    serviceSection.GetSection(methodCategory).GetSection("_all").GetSection("communication"),
                    serviceSection.GetSection(methodCategory).GetSection(methodDefinition.Name).GetSection("communication"));
            }

            var communicator = communicationMethod.CreateCommunicator(communicatorConfig);

            lock (_communicatorMap)
            {
                if (_communicatorMap.TryGetValue(key, out var cachedCommunicator))
                {
                    (communicator as IDisposable)?.Dispose();
                    return cachedCommunicator;
                }

                _communicatorMap.Add(key, communicator);
                return communicator;
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
