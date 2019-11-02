using System;
using Dasync.EETypes;
using Dasync.EETypes.Ioc;
using Dasync.EETypes.Resolvers;
using Dasync.ExecutionEngine.IntrinsicFlow;
using Dasync.Modeling;

namespace Dasync.ExecutionEngine.Resolvers
{
    public class ServiceResolver : IServiceResolver
    {
        private readonly ICommunicationModel _communicationModel;
        private readonly IDomainServiceProvider _domainServiceProvider;

        public ServiceResolver(ICommunicationModel communicationModel, IDomainServiceProvider domainServiceProvider)
        {
            _communicationModel = communicationModel;
            _domainServiceProvider = domainServiceProvider;
        }

        public bool TryResolve(ServiceId serviceId, out IServiceReference serviceReference)
        {
            IServiceDefinition serviceDefinition;

            if (string.Equals(serviceId.Name, IntrinsicCommunicationModel.IntrinsicRoutinesServiceDefinition.Name, StringComparison.OrdinalIgnoreCase))
            {
                serviceDefinition = IntrinsicCommunicationModel.IntrinsicRoutinesServiceDefinition;
            }
            else
            {
                serviceDefinition = _communicationModel.FindServiceByName(serviceId.Name);

                // Convenience resolution if a person typed the "Service" suffix where the full service name matches the class name.
                if (serviceDefinition == null && serviceId.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
                {
                    var candidateServiceName = serviceId.Name.Substring(0, serviceId.Name.Length - 7);
                    serviceDefinition = _communicationModel.FindServiceByName(candidateServiceName);
                    if (serviceDefinition != null && serviceDefinition.Implementation != null &&
                        serviceDefinition.Implementation.Name.Equals(serviceId.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        serviceId = serviceId.Clone();
                        serviceId.Name = candidateServiceName;
                    }
                    else
                    {
                        serviceDefinition = null;
                    }
                }
            }

            if (serviceDefinition == null)
            {
                serviceReference = null;
                return false;
            }

            serviceReference = new ServiceReference(_domainServiceProvider, serviceId, serviceDefinition);
            return true;
        }
    }
}