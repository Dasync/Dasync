using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Persistence;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.ExecutionEngine.Persistence
{
    public class MethodStateStorageProvider : IMethodStateStorageProvider
    {
        private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
        private readonly IConfiguration _configuration;
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;
        private readonly Dictionary<string, IPersistenceMethod> _persistenceMethods;
        private readonly Dictionary<IMethodDefinition, IMethodStateStorage> _storageMap = new Dictionary<IMethodDefinition, IMethodStateStorage>();

        public MethodStateStorageProvider(
            ICommunicationSettingsProvider communicationSettingsProvider,
            IEnumerable<IConfiguration> safeConfiguration,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver,
            IEnumerable<IPersistenceMethod> persistenceMethods)
        {
            _communicationSettingsProvider = communicationSettingsProvider;
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;

            _configuration = safeConfiguration.FirstOrDefault()?.GetSection("dasync")
                ?? (IConfiguration)new ConfigurationRoot(Array.Empty<IConfigurationProvider>());

            _persistenceMethods = persistenceMethods.ToDictionary(m => m.Type, m => m, StringComparer.OrdinalIgnoreCase);
        }

        public IMethodStateStorage GetStorage(ServiceId serviceId, MethodId methodId, bool returnNullIfNotFound = false)
        {
            if (_persistenceMethods.Count == 0)
            {
                if (returnNullIfNotFound)
                    return null;
                else
                    throw new MethodStateStorageNotFoundException("There are no method state storages registered.");
            }

            var serviceRef = _serviceResolver.Resolve(serviceId);
            var methodRef = _methodResolver.Resolve(serviceRef.Definition, methodId);

            lock (_storageMap)
            {
                if (_storageMap.TryGetValue(methodRef.Definition, out var cachedStorage))
                    return cachedStorage;
            }

            var serviceCategory = serviceRef.Definition.Type == ServiceType.External ? "_external" : "_local";
            var methodCategory = methodRef.Definition.IsQuery ? "queries" : "commands";

            var persistenceType = _communicationSettingsProvider.GetMethodSettings(methodRef.Definition).PersistenceType;

            IPersistenceMethod persistenceMethod;
            if (string.IsNullOrWhiteSpace(persistenceType))
            {
                if (_persistenceMethods.Count == 1)
                {
                    persistenceMethod = _persistenceMethods.First().Value;
                }
                else
                {
                    if (returnNullIfNotFound)
                        return null;
                    else
                        throw new MethodStateStorageNotFoundException("Multiple method state storages are available.");
                }
            }
            else
            {
                if (!_persistenceMethods.TryGetValue(persistenceType, out persistenceMethod))
                {
                    if (returnNullIfNotFound)
                        return null;
                    else
                        throw new MethodStateStorageNotFoundException($"Method state storage '{persistenceType}' is not registered.");
                }
            }

            var servicesSection = _configuration.GetSection("services");
            var serviceSection = servicesSection.GetSection(serviceRef.Definition.Name);

            var storageConfig = GetConfiguraion(
                _configuration.GetSection("persistence"),
                _configuration.GetSection(methodCategory).GetSection("persistence"),
                servicesSection.GetSection(serviceCategory).GetSection("persistence"),
                servicesSection.GetSection(serviceCategory).GetSection(methodCategory).GetSection("persistence"),
                serviceSection.GetSection("persistence"),
                serviceSection.GetSection(methodCategory).GetSection("_all").GetSection("persistence"),
                serviceSection.GetSection(methodCategory).GetSection(methodRef.Definition.Name).GetSection("persistence"));

            var storage = persistenceMethod.CreateMethodStateStorage(storageConfig);

            lock (_storageMap)
            {
                if (_storageMap.TryGetValue(methodRef.Definition, out var cachedStorage))
                {
                    (storage as IDisposable)?.Dispose();
                    return cachedStorage;
                }

                _storageMap.Add(methodRef.Definition, storage);
                return storage;
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
