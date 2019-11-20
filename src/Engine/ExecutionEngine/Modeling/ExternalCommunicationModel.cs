using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.EETypes;
using Dasync.Modeling;

namespace Dasync.ExecutionEngine.Modeling
{
    public interface IExternalCommunicationModel : ICommunicationModel
    {
        IExternalServiceDefinition GetOrAddService(ServiceId serviceId);
    }

    public class ExternalCommunicationModel : PropertyBag, IExternalCommunicationModel, IMutableCommunicationModel
    {
        private readonly List<ExternalServiceDefinition> _services = new List<ExternalServiceDefinition>();
        private readonly List<IEntityProjectionDefinition> _projections = new List<IEntityProjectionDefinition>();

        public IReadOnlyCollection<IServiceDefinition> Services => _services;

        public IReadOnlyCollection<IEntityProjectionDefinition> EntityProjections => _projections;

        IReadOnlyCollection<IMutableServiceDefinition> IMutableCommunicationModel.Services => _services;

        public IEntityProjectionDefinition FindEntityProjectionByIterfaceType(Type interfaceType) => null;

        public IServiceDefinition FindServiceByImplementation(Type implementationType) => null;

        public IServiceDefinition FindServiceByInterface(Type interfaceType) => null;

        public IServiceDefinition FindServiceByName(string name)
        {
            lock (_services)
            {
                return _services.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public IExternalServiceDefinition GetOrAddService(ServiceId serviceId)
        {
            lock (_services)
            {
                var existingDefinition = FindServiceByName(serviceId.Proxy ?? serviceId.Name);
                if (existingDefinition != null)
                    return (IExternalServiceDefinition)existingDefinition;

                var newDefinition = new ExternalServiceDefinition(this, serviceId);
                _services.Add(newDefinition);
                return newDefinition;
            }
        }
    }
}
