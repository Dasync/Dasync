using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Configuration;
using Dasync.EETypes.Resolvers;
using Dasync.ExecutionEngine.Modeling;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.ExecutionEngine.Communication
{
    public class CommunicatorProvider : ICommunicatorProvider
    {
        private const string CommunicationSectionName = "communication";

        private readonly ICommunicationModelConfiguration _communicationModelConfiguration;
        private readonly IConfiguration _configuration;
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;
        private readonly IExternalCommunicationModel _externalCommunicationModel;
        private readonly Dictionary<string, ICommunicationMethod> _communicationMethods;
        private readonly Dictionary<object, ICommunicator> _communicatorMap = new Dictionary<object, ICommunicator>();

        public CommunicatorProvider(
            ICommunicationModelConfiguration communicationModelConfiguration,
            IEnumerable<IConfiguration> safeConfiguration,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver,
            IExternalCommunicationModel externalCommunicationModel,
            IEnumerable<ICommunicationMethod> communicationMethods)
        {
            _communicationModelConfiguration = communicationModelConfiguration;
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

            // TODO: cache by methodId/serviceId
            var (serviceDefinition, methodDefinition) = Resolve(serviceId, methodId, assumeExternal);

            var key = (object)methodDefinition ?? serviceDefinition;
            lock (_communicatorMap)
            {
                if (_communicatorMap.TryGetValue(key, out var cachedCommunicator))
                    return cachedCommunicator;
            }

            IConfiguration communicatorConfig = null;

            if (methodDefinition != null)
            {
                var configOverrideLevels = _communicationModelConfiguration.GetMethodOverrideLevels(methodDefinition, CommunicationSectionName);
                if (configOverrideLevels.HasFlag(ConfigOverrideLevels.Primitive) ||
                    (configOverrideLevels.HasFlag(ConfigOverrideLevels.ServicePrimitives) && methodDefinition.IsQuery)) // TODO: service communicator per commands/queries
                {
                    communicatorConfig = _communicationModelConfiguration.GetMethodConfiguration(methodDefinition, CommunicationSectionName);
                }
                else
                {
                    lock (_communicatorMap)
                    {
                        if (_communicatorMap.TryGetValue(serviceDefinition, out var cachedCommunicator))
                        {
                            _communicatorMap[methodDefinition] = cachedCommunicator;
                            return cachedCommunicator;
                        }
                    }

                    methodDefinition = null;
                    key = serviceDefinition;
                }
            }

            if (methodDefinition == null)
            {
                communicatorConfig = _communicationModelConfiguration.GetCommandsConfiguration(serviceDefinition, CommunicationSectionName);
            }

            var communicationType = GetCommunicationType(communicatorConfig);

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

        private static string GetCommunicationType(IConfiguration config) => config.GetSection("type").Value ?? "";

        private struct ServiceAndMethodDefinitions
        {
            public IServiceDefinition Service { get; set; }

            public IMethodDefinition Method { get; set; }

            public void Deconstruct(out IServiceDefinition serviceDefinition, out IMethodDefinition methodDefinition)
            {
                serviceDefinition = Service;
                methodDefinition = Method;
            }
        }

        private ServiceAndMethodDefinitions Resolve(ServiceId serviceId, MethodId methodId, bool assumeExternal = false)
        {
            var result = new ServiceAndMethodDefinitions();

            if (_serviceResolver.TryResolve(serviceId, out var serviceRef))
            {
                result.Service = serviceRef.Definition;

                // NOTE: system services are not unique within a multi-service ecosystem, thus must
                // use the configuration of the calling (proxy) service without any specific method.
                // Otherwise, a continuation can be sent to a wrong instance of a system service.
                if (result.Service.Type == ServiceType.System && !string.IsNullOrEmpty(serviceId.Proxy))
                {
                    return Resolve(new ServiceId { Name = serviceId.Proxy }, null, assumeExternal);
                }

                result.Method = methodId == null ? null : _methodResolver.Resolve(result.Service, methodId).Definition;
            }
            else if (assumeExternal)
            {
                var externalServiceDefinition = _externalCommunicationModel.GetOrAddService(serviceId);
                var externalMethodDefinition = methodId == null ? null : externalServiceDefinition.GetOrAddMethod(methodId);
                result.Service = externalServiceDefinition;
                result.Method = externalMethodDefinition;
            }
            else
            {
                throw new ServiceResolveException(serviceId);
            }

            return result;
        }
    }
}
