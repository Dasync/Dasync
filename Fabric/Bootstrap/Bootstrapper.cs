using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Fabric;
using Dasync.EETypes.Proxy;
using Dasync.Ioc;
using Dasync.ServiceRegistry;

namespace Dasync.Bootstrap
{
    public class BootstrapResult
    {
        public IFabric Fabric { get; set; }

        public IAppServiceIocContainer AppIocContainer { get; set; }
    }

    public class Bootstrapper
    {
        private IAppIocContainerProvider[] _appIocContainerProviders;
        //private IAppServiceDiscoveryFromCodeMarkup _appServiceDiscoveryFromCodeMarkup;
        //private IAppServiceRegistrationInfoExtractor _appServiceRegistrationInfoExtractor;
        //private IAppServiceDiscoveryFromRuntimeCollection _appServiceDiscoveryFromRuntimeCollection;
        private IServiceRegistry _serviceRegistry;
        private AppServiceIocContainerProxy.Holder _appIocContainerHolder;
        private IServiceProxyBuilder _serviceProxyBuilder;
        private IFabric _fabric;
        private IServicePublisher[] _servicePublishers;
        private readonly IServiceRegistryUpdaterViaDiscovery _serviceRegistryUpdaterViaDiscovery;

        public Bootstrapper(
            IAppIocContainerProvider[] appIocContainerProviders,
            //IAppServiceDiscoveryFromCodeMarkup appServiceDiscoveryFromCodeMarkup,
            //IAppServiceRegistrationInfoExtractor appServiceRegistrationInfoExtractor,
            //IAppServiceDiscoveryFromRuntimeCollection appServiceDiscoveryFromRuntimeCollection,
            IServiceRegistry serviceRegistry,
            AppServiceIocContainerProxy.Holder appIocContainerHolder,
            IServiceProxyBuilder serviceProxyBuilder,
            IFabric[] registeredFabrics,
            ICurrentFabric currentFabricHolder,
            IServicePublisher[] servicePublishers,
            IServiceRegistryUpdaterViaDiscovery serviceRegistryUpdaterViaDiscovery)
        {
            _appIocContainerProviders = appIocContainerProviders;
            //_appServiceDiscoveryFromCodeMarkup = appServiceDiscoveryFromCodeMarkup;
            //_appServiceRegistrationInfoExtractor = appServiceRegistrationInfoExtractor;
            //_appServiceDiscoveryFromRuntimeCollection = appServiceDiscoveryFromRuntimeCollection;
            _serviceRegistry = serviceRegistry;
            _appIocContainerHolder = appIocContainerHolder;
            _serviceProxyBuilder = serviceProxyBuilder;

            if (registeredFabrics.Length > 1)
                throw new InvalidOperationException("Multi-fabric is not supported.");

            if (registeredFabrics.Length == 1)
            {
                _fabric = registeredFabrics[0];
                ((ICurrentFabricSetter)currentFabricHolder).SetInstance(_fabric);
            }

            _servicePublishers = servicePublishers;
            _serviceRegistryUpdaterViaDiscovery = serviceRegistryUpdaterViaDiscovery;
        }

        public async Task<BootstrapResult> BootstrapAsync(CancellationToken ct)
        {
            //_serviceRegistry.Register(
            //    new ServiceRegistrationInfo
            //    {
            //        Name = nameof(IntrinsicRoutines),
            //        QualifiedServiceTypeName = typeof(IntrinsicRoutines).AssemblyQualifiedName,
            //        QualifiedImplementationTypeName = typeof(IntrinsicRoutines).AssemblyQualifiedName,
            //        IsSingleton = true
            //    });

            //foreach (var serviceType in _appServiceDiscoveryFromCodeMarkup.DiscoverServiceTypes())
            //{
            //    ServiceRegistrationInfo registrationInfo;
            //    try
            //    {
            //        registrationInfo = _appServiceRegistrationInfoExtractor.Extract(serviceType);
            //    }
            //    catch
            //    {
            //        continue;
            //    }
            //    // TODO: fill in 'fabric' config?
            //    _serviceRegistry.Register(registrationInfo);
            //}

            //foreach (var registrationInfo in _appServiceDiscoveryFromRuntimeCollection.Services)
            //{
            //    // TODO: fill in 'fabric' config?
            //    _serviceRegistry.Register(registrationInfo);
            //}

            var appIocContainer = _appIocContainerProviders
                .Select(p => p.GetAppIocContainer())
                .FirstOrDefault(c => c != null);

            if (appIocContainer != null)
            {
                foreach (var bindingInfo in appIocContainer.DiscoverServices())
                {
                    var registrationInfo = new ServiceRegistrationInfo
                    {
                        QualifiedServiceTypeName = bindingInfo.ServiceType.AssemblyQualifiedName,
                        QualifiedImplementationTypeName = bindingInfo.ImplementationType?.AssemblyQualifiedName,
                        IsExternal = bindingInfo.IsExternal,
                        IsSingleton = true
                    };
                    // TODO: fill in 'fabric' config?
                    _serviceRegistry.Register(registrationInfo);
                }
            }

            if (appIocContainer == null)
                appIocContainer = new BasicAppServiceIocContainer();
            _appIocContainerHolder.Container = appIocContainer;

            foreach (var serviceRegistration in _serviceRegistry.AllRegistrations)
            {
                if (serviceRegistration.IsSingleton)
                {
                    var implementationType = serviceRegistration.ImplementationType;

                    if (implementationType == null && appIocContainer.TryGetImplementationType(
                        serviceRegistration.ServiceType, out implementationType) == true)
                    {
                        // TODO: properly update registration
                        ((ServiceRegistration)serviceRegistration).ImplementationType = implementationType;
                    }

                    if (!serviceRegistration.IsExternal && implementationType == null)
                        throw new InvalidOperationException(
                            $"Could not find implementation type for service '{serviceRegistration.ServiceType}'.");

                    Func<object> proxyFactory = () =>
                    {
                        var serviceId = new ServiceId
                        {
                            ServiceName = serviceRegistration.ServiceName
                        };
                        var proxy = _serviceProxyBuilder.Build(serviceId);
                        return proxy;
                    };

                    appIocContainer.RebindService(
                        serviceRegistration.ServiceType,
                        proxyFactory);

                    if (implementationType != null)
                    {
                        appIocContainer.RebindService(
                            implementationType,
                            proxyFactory);
                    }
                }
            }

            if (_fabric != null)
            {
                await _fabric.InitializeAsync(ct);
            }

            await _serviceRegistryUpdaterViaDiscovery.UpdateAsync(ct);

            if (_fabric != null && _servicePublishers.Length > 0)
            {
                var servicesToRegister = new List<ServiceRegistrationInfo>();

                foreach (var serviceRegistration in _serviceRegistry.AllRegistrations.Where(r => !r.IsExternal))
                {
                    var serviceId = new ServiceId { ServiceName = serviceRegistration.ServiceName };
                    if (_fabric.GetConnector(serviceId) is
                        IFabricConnectorWithConfiguration connectorWithConfiguration)
                    {
                        servicesToRegister.Add(new ServiceRegistrationInfo
                        {
                            Name = serviceRegistration.ServiceName,
                            QualifiedServiceTypeName = serviceRegistration.ServiceType.FullName,
                            IsSingleton = serviceRegistration.IsSingleton,
                            IsExternal = true,
                            ConnectorType = connectorWithConfiguration.ConnectorType,
                            ConnectorConfiguration = connectorWithConfiguration.Configuration
                        });
                    }
                }

                if (servicesToRegister.Count > 0)
                {
                    foreach (var publisher in _servicePublishers)
                    {
                        await publisher.PublishAsync(servicesToRegister, ct);
                    }
                }
            }

            if (_fabric != null)
            {
                await _fabric.StartAsync(ct);
            }

            return new BootstrapResult
            {
                Fabric = _fabric,
                AppIocContainer = _appIocContainerHolder.Container
            };
        }
    }
}
