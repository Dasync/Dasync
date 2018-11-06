using System;
using System.Collections.Generic;
using System.Linq;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Proxy;
using Dasync.Ioc;
using Dasync.Proxy;
using Dasync.ServiceRegistry;

namespace Dasync.ExecutionEngine.Proxy
{
    public class ServiceProxyBuilder : IServiceProxyBuilder, ISerializedServiceProxyBuilder
    {
        private readonly IServiceRegistry _serviceRegistry;
        private readonly IProxyTypeBuilder _proxyTypeBuilder;
        private readonly IProxyMethodExecutor _proxyMethodExecutor;
        private readonly IAppServiceIocContainer _appServiceIocContainer;

        public ServiceProxyBuilder(
            IServiceRegistry serviceRegistry,
            IProxyTypeBuilder proxyTypeBuilder,
            IProxyMethodExecutor proxyMethodExecutor,
            IAppServiceIocContainer appServiceIocContainer,
            ISerializedServiceProxyBuilder holder)
        {
            ((SerializedServiceProxyBuilderHolder)holder).Builder = this;
            _serviceRegistry = serviceRegistry;
            _proxyTypeBuilder = proxyTypeBuilder;
            _proxyMethodExecutor = proxyMethodExecutor;
            _appServiceIocContainer = appServiceIocContainer;
        }

        public object Build(ServiceId serviceId) => Build(serviceId, null);

        public object Build(ServiceId serviceId, string[] additionalInterfaces)
        {
            var registration = _serviceRegistry.AllRegistrations
                .SingleOrDefault(r => r.ServiceName == serviceId.ServiceName);

            Type proxyType;

            if (registration?.ImplementationType != null)
            {
                proxyType = _proxyTypeBuilder.Build(registration.ImplementationType);
            }
            else
            {
                var interfacesTypes = new List<Type>(additionalInterfaces?.Length ?? 1);

                if (registration?.ServiceType != null)
                    interfacesTypes.Add(registration.ServiceType);

                if (additionalInterfaces != null)
                {
                    foreach (var typeFullName in additionalInterfaces)
                    {
#warning Use better type resolver?
                        var type = Type.GetType(typeFullName, throwOnError: false);
                        if (type != null)
                            interfacesTypes.Add(type);
                    }
                }

                proxyType = _proxyTypeBuilder.Build(interfacesTypes);
            }

            var allInterfaces = proxyType
                .GetInterfaces()
                .Where(i => i != typeof(IProxy))
                .Select(i => i.AssemblyQualifiedName);

            if (additionalInterfaces != null)
                allInterfaces = allInterfaces.Union(additionalInterfaces).Distinct();

            var serviceProxyContext = new ServiceProxyContext
            {
                Service = new ServiceDescriptor
                {
                    Id = serviceId,
                    Interfaces = allInterfaces.ToArray()
                }
            };

            var buildingContext = ServiceProxyBuildingContext.EnterScope(serviceProxyContext);
            try
            {
#warning Needs ability to inject services with different Service IDs (parent Service ID?) as a part of service mesh framework.
                var proxy = (IProxy)_appServiceIocContainer.Resolve(proxyType);
                proxy.Executor = _proxyMethodExecutor;
                proxy.Context = serviceProxyContext;
                return proxy;
            }
            finally
            {
                buildingContext.ExitScope();
            }
        }
    }

    /// <summary>
    /// 
    /// WARNING
    /// 
    /// Introduced to break the cyclic dependency between the Proxy Method Executor and the Proxy Serializer:
    /// the executor needs the serializer to serialize input, and the serializer needs executor to build service
    /// proxies on deserialization.
    /// 
    /// Is there a better design?
    /// </summary>
    public class SerializedServiceProxyBuilderHolder : ISerializedServiceProxyBuilder
    {
        public ISerializedServiceProxyBuilder Builder { get; set; }

        public object Build(ServiceId serviceId, string[] additionalInterfaces)
        {
            if (Builder == null)
                throw new InvalidOperationException();
            return Builder.Build(serviceId, additionalInterfaces);
        }
    }
}
