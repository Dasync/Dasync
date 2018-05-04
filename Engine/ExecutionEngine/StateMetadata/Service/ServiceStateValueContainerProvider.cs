using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dasync.Proxy;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.StateMetadata.Service
{
    public interface IServiceStateValueContainerProvider
    {
        IValueContainerProxyFactory GetContainerFactory(Type serviceType);
    }

    public class ServiceStateValueContainerProvider : IServiceStateValueContainerProvider
    {
        private readonly IServiceStateMetadataProvider _serviceStateMetadataProvider;

        private readonly Dictionary<Type, IValueContainerProxyFactory> _containerFactoryMap =
            new Dictionary<Type, IValueContainerProxyFactory>();

        private static readonly IValueContainerProxyFactory _emptyValueContainerProxyFactory =
            new EmptyValueContainerProxyFactory();

        public ServiceStateValueContainerProvider(
            IServiceStateMetadataProvider serviceStateMetadataProvider)
        {
            _serviceStateMetadataProvider = serviceStateMetadataProvider;
        }

        public IValueContainerProxyFactory GetContainerFactory(Type serviceType)
        {
            if (typeof(IProxy).IsAssignableFrom(serviceType))
                serviceType = serviceType.GetBaseType();

            lock (_containerFactoryMap)
            {
                if (!_containerFactoryMap.TryGetValue(serviceType, out var factory))
                {
                    factory = CreateFactory(serviceType);
                    _containerFactoryMap.Add(serviceType, factory);
                }
                return factory;
            }
        }

        private IValueContainerProxyFactory CreateFactory(Type serviceType)
        {
            var metadata = _serviceStateMetadataProvider.GetMetadata(serviceType);

            if (metadata.Variables.Length == 0)
                return _emptyValueContainerProxyFactory;

            var stateMembers = metadata.Variables.Select(
                v => new KeyValuePair<string, MemberInfo>(v.Name, v.Field));

            return ValueContainerFactory.GetProxyFactory(serviceType, stateMembers);
        }

        private sealed class EmptyValueContainerProxyFactory : IValueContainerProxyFactory
        {
            private readonly IValueContainer _emptyContainer = new EmptyValueContainer();

            public IValueContainer Create(object target) => _emptyContainer;
        }
    }

    public static class ServiceStateValueContainerProviderExtensions
    {
        public static IValueContainer CreateContainer(
            this IServiceStateValueContainerProvider serviceStateValueContainerFactory,
            object serviceInstance)
        {
            var serviceType = serviceInstance.GetType();
            var containerFactory = serviceStateValueContainerFactory.GetContainerFactory(serviceType);
            return containerFactory.Create(serviceInstance);
        }
    }
}
