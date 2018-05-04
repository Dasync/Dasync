using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using Autofac.Features.ResolveAnything;

namespace Dasync.Ioc.Autofac
{
    public sealed class AutofaceIocContainerAdapter : IIocContainer, IAppServiceIocContainer
    {
        private sealed class DynamicServiceModule
        {
            private Dictionary<Type, Func<IComponentContext, object>> _factories;
            private HashSet<Type> _selfBoundTypes;

            public readonly Action<ContainerBuilder> ConfigurationAction;

            public DynamicServiceModule()
            {
                ConfigurationAction = Load;
            }

            public void Load(ContainerBuilder builder)
            {
                if (_factories != null)
                {
                    foreach (var pair in _factories)
                    {
                        builder.Register(pair.Value).As(pair.Key).ExternallyOwned();
                    }
                }

                if (_selfBoundTypes != null)
                {
                    foreach (var type in _selfBoundTypes)
                    {
                        builder.RegisterType(type).AsSelf().ExternallyOwned();
                    }
                }
            }

            public void RegisterFactory(Type serviceType, Func<object> factory)
            {
                if (_factories == null)
                    _factories = new Dictionary<Type, Func<IComponentContext, object>>();
                _factories[serviceType] = ctx => factory();
            }

            public void RegisterSelf(Type serviceType)
            {
                if (_selfBoundTypes == null)
                    _selfBoundTypes = new HashSet<Type>();
                _selfBoundTypes.Add(serviceType);
            }

            public bool IsRegistered(Type serviceType)
            {
                if (_factories != null && _factories.ContainsKey(serviceType))
                    return true;
                if (_selfBoundTypes != null && _selfBoundTypes.Contains(serviceType))
                    return true;
                return false;
            }
        }

        private readonly DynamicServiceModule _serviceModule = new DynamicServiceModule();
        private ILifetimeScope _serviceScope;

        [ThreadStatic]
        private ILifetimeScope _scope;

        public AutofaceIocContainerAdapter(IContainer container)
        {
            Container = container;
        }

        public IContainer Container { get; private set; }

        public object Resolve(Type serviceType)
        {
            if (_serviceScope == null)
            {
                lock (_serviceModule)
                {
                    if (_serviceScope == null)
                    {
                        _serviceScope = Container.BeginLifetimeScope(
                            _serviceModule.ConfigurationAction);

                        _serviceScope.ComponentRegistry.AddRegistrationSource(
                            new AnyConcreteTypeNotAlreadyRegisteredSource());
                    }
                }
            }

            return _serviceScope.Resolve(serviceType);
        }

        public IEnumerable<ServiceBindingInfo> DiscoverServices()
        {
            var result = new List<ServiceBindingInfo>();

            foreach (var registraion in Container.ComponentRegistry.Registrations)
            {
                if (registraion.Metadata.TryGetValue(nameof(ServiceBindingInfo), out var value)
                    && value is ServiceBindingInfo serviceBindingInfo)
                {
                    result.Add(serviceBindingInfo);
                }
            }

            return result;
        }

        public void RebindService(Type serviceType, Func<object> serviceImplementationProvider)
        {
            _serviceModule.RegisterFactory(serviceType, serviceImplementationProvider);
        }

        public bool TryGetImplementationType(Type serviceType, out Type implementationType)
        {
            implementationType = null;

            if (!Container.ComponentRegistry.TryGetRegistration(
                new TypedService(serviceType), out var registration))
                return false;

            if (registration.Activator is ReflectionActivator activator)
            {
                implementationType = activator.LimitType;
                return true;
            }

            return false;
        }
    }
}
