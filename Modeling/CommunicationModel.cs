using System;
using System.Collections.Generic;

namespace Dasync.Modeling
{
    public class CommunicationModel : PropertyBag, IMutableCommunicationModel, ICommunicationModel, IMutablePropertyBag, IPropertyBag
    {
        private readonly HashSet<ServiceDefinition> _services = new HashSet<ServiceDefinition>();

        private readonly Dictionary<string, ServiceDefinition> _servicesByName =
            new Dictionary<string, ServiceDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<Type, ServiceDefinition> _servicesByInterfaceType =
            new Dictionary<Type, ServiceDefinition>();

        private readonly Dictionary<Type, ServiceDefinition> _servicesByImplemntationType =
            new Dictionary<Type, ServiceDefinition>();

        private readonly HashSet<EntityProjectionDefinition> _entityProjections = new HashSet<EntityProjectionDefinition>();

        private readonly Dictionary<Type, EntityProjectionDefinition> _entityProjectionsByInterfaceType =
            new Dictionary<Type, EntityProjectionDefinition>();

        public IReadOnlyCollection<IServiceDefinition> Services => _services;

        public IServiceDefinition FindServiceByName(string name) =>
            _servicesByName.TryGetValue(name, out var serviceDefinition) ? serviceDefinition : null;

        public IServiceDefinition FindServiceByInterface(Type interfaceType) =>
            _servicesByInterfaceType.TryGetValue(interfaceType, out var serviceDefinition) ? serviceDefinition : null;

        public IServiceDefinition FindServiceByImplementation(Type implementationType) =>
            _servicesByImplemntationType.TryGetValue(implementationType, out var serviceDefinition) ? serviceDefinition : null;

        internal void OnServiceNameChanging(ServiceDefinition serviceDefinition, string newName)
        {
            if (FindServiceByName(newName) != null)
                throw new InvalidOperationException($"Cannot rename service '{serviceDefinition.Name}' to '{newName}' because another service already exist under the same name.");

            if (serviceDefinition.Name != null)
                _servicesByName.Remove(serviceDefinition.Name);

            if (newName != null)
            {
                _services.Add(serviceDefinition);
                _servicesByName.Add(newName, serviceDefinition);
            }
        }

        internal void OnServiceImplementaionChanging(ServiceDefinition serviceDefinition, Type newImplementationType)
        {
            if (FindServiceByImplementation(newImplementationType) != null)
                throw new InvalidOperationException($"Multiple services cannot share the same implementation type '{newImplementationType}'.");

            if (serviceDefinition.Implementation != null)
                _servicesByImplemntationType.Remove(serviceDefinition.Implementation);

            if (newImplementationType != null)
            {
                _services.Add(serviceDefinition);
                _servicesByImplemntationType.Add(newImplementationType, serviceDefinition);
            }
        }

        internal void OnServiceInterfaceRemoved(ServiceDefinition serviceDefinition, Type removedInterfaceType)
        {
            _servicesByInterfaceType.Remove(removedInterfaceType);
        }

        internal void OnServiceInterfaceAdding(ServiceDefinition serviceDefinition, Type newInterfaceType)
        {
            if (FindServiceByInterface(newInterfaceType) != null)
                throw new InvalidOperationException($"Multiple services cannot share the same interface type '{newInterfaceType}'.");

            _services.Add(serviceDefinition);
            _servicesByInterfaceType.Add(newInterfaceType, serviceDefinition);
        }

        public IEntityProjectionDefinition FindEntityProjectionByIterfaceType(Type interfaceType) =>
            _entityProjectionsByInterfaceType.TryGetValue(interfaceType, out var definition) ? definition : null;

        public IReadOnlyCollection<IEntityProjectionDefinition> EntityProjections => _entityProjections;

        internal void OnEntityProjectionInterfaceSet(EntityProjectionDefinition definition)
        {
            _entityProjections.Add(definition);
            _entityProjectionsByInterfaceType.Add(definition.InterfaceType, definition);
        }
    }
}
