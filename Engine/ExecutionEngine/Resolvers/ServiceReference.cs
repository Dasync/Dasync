using System;
using Dasync.EETypes;
using Dasync.EETypes.Ioc;
using Dasync.EETypes.Resolvers;
using Dasync.Modeling;

namespace Dasync.ExecutionEngine.Resolvers
{
    public class ServiceReference : IServiceReference
    {
        private readonly IDomainServiceProvider _domainServiceProvider;

        public ServiceReference(IDomainServiceProvider domainServiceProvider,
            ServiceId serviceId, IServiceDefinition serviceDefinition)
        {
            _domainServiceProvider = domainServiceProvider;
            Id = serviceId;
            Definition = serviceDefinition;
        }

        public ServiceId Id { get; }

        public IServiceDefinition Definition { get; }

        public object GetInstance()
        {
            if (Definition.Implementation != null)
                return _domainServiceProvider.GetService(Definition.Implementation);

            if (Definition.Interfaces?.Length < 1)
                throw new InvalidOperationException($"The service '{Definition.Name}' has no implementation nor interfaces.");

            return _domainServiceProvider.GetService(Definition.Interfaces[0]); // the proxy implements all interfaces
        }
    }
}
