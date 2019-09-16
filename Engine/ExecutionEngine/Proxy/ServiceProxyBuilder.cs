using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Ioc;
using Dasync.EETypes.Proxy;
using Dasync.Modeling;
using Dasync.Proxy;

namespace Dasync.ExecutionEngine.Proxy
{
    public class ServiceProxyBuilder : IServiceProxyBuilder, ISerializedServiceProxyBuilder
    {
        private readonly ICommunicationModel _communicationModel;
        private readonly IProxyTypeBuilder _proxyTypeBuilder;
        private readonly IProxyMethodExecutor _proxyMethodExecutor;
        private readonly IDomainServiceProvider _domainServiceProvider;

        public ServiceProxyBuilder(
            ICommunicationModel communicationModel,
            IProxyTypeBuilder proxyTypeBuilder,
            IProxyMethodExecutor proxyMethodExecutor,
            IDomainServiceProvider domainServiceProvider,
            ISerializedServiceProxyBuilder holder)
        {
            ((SerializedServiceProxyBuilderHolder)holder).Builder = this;
            _communicationModel = communicationModel;
            _proxyTypeBuilder = proxyTypeBuilder;
            _proxyMethodExecutor = proxyMethodExecutor;
            _domainServiceProvider = domainServiceProvider;
        }

        public object Build(ServiceId serviceId) => Build(serviceId, null);

        public object Build(ServiceId serviceId, string[] additionalInterfaces)
        {
            var serviceDefinition = _communicationModel.FindServiceByName(serviceId.Name);
            if (serviceDefinition == null)
                throw new InvalidOperationException($"The service '{serviceId.Name}' is not registered.");

            return CreateServiceProxy(serviceDefinition, serviceId, additionalInterfaces);
        }

        private object CreateServiceProxy(IServiceDefinition serviceDefinition, ServiceId serviceId, string[] additionalInterfaces)
        {
            Type proxyType;

            if (serviceDefinition.Implementation != null)
            {
                proxyType = _proxyTypeBuilder.Build(serviceDefinition.Implementation);
            }
            else
            {
                var interfacesTypes = new HashSet<Type>();

                if (serviceDefinition.Interfaces?.Length > 0)
                {
                    foreach (var interfaceType in serviceDefinition.Interfaces)
                    {
                        interfacesTypes.Add(interfaceType);
                    }
                }

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
                Definition = serviceDefinition,

                Descriptor = new ServiceDescriptor
                {
                    Id = serviceId,
                    Interfaces = allInterfaces.ToArray()
                }
            };

            var buildingContext = ServiceProxyBuildingContext.EnterScope(serviceProxyContext);
            try
            {
                var proxy = (IProxy)ActivateServiceProxyInstance(proxyType);
                proxy.Executor = _proxyMethodExecutor;
                proxy.Context = serviceProxyContext;
                return proxy;
            }
            finally
            {
                buildingContext.ExitScope();
            }
        }

        private object ActivateServiceProxyInstance(Type type)
        {
            var ctorInfo = SelectConstructor(type);
            var parametersInfo = ctorInfo.GetParameters();
            var parameterValues = new object[parametersInfo.Length];

            for (var i = 0; i < parametersInfo.Length; i++)
            {
                var parameterInfo = parametersInfo[i];
                var parameterValue = _domainServiceProvider.GetService(parameterInfo.ParameterType);
                parameterValues[i] = parameterValue;
            }

            return ctorInfo.Invoke(parameterValues);
        }

        protected virtual ConstructorInfo SelectConstructor(Type type)
        {
            foreach (var ctorInfo in type.GetTypeInfo().DeclaredConstructors)
            {
                if (ctorInfo.IsPublic)
                    return ctorInfo;
            }
            throw new InvalidOperationException(
                $"Could not find a constructor to create an instance of '{type}'.");
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
                throw new InvalidOperationException("The engine is not properly initialized.");
            return Builder.Build(serviceId, additionalInterfaces);
        }
    }
}
