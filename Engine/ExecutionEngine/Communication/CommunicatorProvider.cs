using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Resolvers;
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
        private readonly Dictionary<string, ICommunicationMethod> _communicationMethods;
        private readonly Dictionary<IMethodDefinition, ICommunicator> _communicatorMap = new Dictionary<IMethodDefinition, ICommunicator>();

        public CommunicatorProvider(
            ICommunicationSettingsProvider communicationSettingsProvider,
            IEnumerable<IConfiguration> safeConfiguration,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver,
            IEnumerable<ICommunicationMethod> communicationMethods)
        {
            _communicationSettingsProvider = communicationSettingsProvider;
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;

            _configuration = safeConfiguration.FirstOrDefault()?.GetSection("dasync")
                ?? (IConfiguration)new ConfigurationRoot(Array.Empty<IConfigurationProvider>());

            _communicationMethods = communicationMethods.ToDictionary(m => m.Type, m => m, StringComparer.OrdinalIgnoreCase);
        }

        public ICommunicator GetCommunicator(ServiceId serviceId, MethodId methodId)
        {
            if (_communicationMethods.Count == 0)
                throw new CommunicationMethodNotFoundException("There are no communication methods registered.");

            var serviceRef = _serviceResolver.Resolve(serviceId);
            var methodRef = _methodResolver.Resolve(serviceRef.Definition, methodId);

            lock (_communicatorMap)
            {
                if (_communicatorMap.TryGetValue(methodRef.Definition, out var cachedCommunicator))
                    return cachedCommunicator;
            }

            var serviceCategory = serviceRef.Definition.Type == ServiceType.External ? "_external" : "_local";
            var methodCategory = methodRef.Definition.IsQuery ? "queries" : "commands";

            var communicationType = _communicationSettingsProvider.GetMethodSettings(methodRef.Definition).CommunicationType;

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
            var serviceSection = servicesSection.GetSection(serviceRef.Definition.Name);

            var communicatorConfig = GetConfiguraion(
                _configuration.GetSection("communication"),
                _configuration.GetSection(methodCategory).GetSection("communication"),
                servicesSection.GetSection(serviceCategory).GetSection("communication"),
                servicesSection.GetSection(serviceCategory).GetSection(methodCategory).GetSection("communication"),
                serviceSection.GetSection("communication"),
                serviceSection.GetSection(methodCategory).GetSection("_all").GetSection("communication"),
                serviceSection.GetSection(methodCategory).GetSection(methodRef.Definition.Name).GetSection("communication"));

            var communicator = communicationMethod.CreateCommunicator(communicatorConfig);

            lock (_communicatorMap)
            {
                if (_communicatorMap.TryGetValue(methodRef.Definition, out var cachedCommunicator))
                {
                    (communicator as IDisposable)?.Dispose();
                    return cachedCommunicator;
                }

                _communicatorMap.Add(methodRef.Definition, communicator);
                return communicator;
            }
        }

        public ICommunicator GetCommunicator(ServiceId serviceId, EventId methodId)
        {
            if (_communicationMethods.Count == 0)
                throw new CommunicationMethodNotFoundException("There are no communication methods registered.");

            throw new NotImplementedException();
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
