using System;
using Dasync.EETypes;
using Dasync.EETypes.Proxy;
using Dasync.Proxy;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.EETypes
{
    public sealed class ServiceProxySerializer : IObjectDecomposer, IObjectComposer
    {
        private readonly ISerializedServiceProxyBuilder _serviceProxyBuilder;

        public ServiceProxySerializer(ISerializedServiceProxyBuilder serviceProxyBuilder)
        {
            _serviceProxyBuilder = serviceProxyBuilder;
        }

        public IValueContainer Decompose(object value)
        {
            var serviceProxy = (IProxy)value;
            var proxyContext = (ServiceProxyContext)serviceProxy.Context;

            return new ServiceProxyContainer
            {
                ServiceId = proxyContext.Descriptor.Id,
                Interfaces = proxyContext.Descriptor.Interfaces
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var values = (ServiceProxyContainer)container;
            var proxy = _serviceProxyBuilder.Build(values.ServiceId, values.Interfaces);
            return proxy;
        }

        public IValueContainer CreatePropertySet(Type valueType) => new ServiceProxyContainer();
    }

    public sealed class ServiceProxyContainer : ValueContainerBase, IValueContainerWithTypeInfo
    {
        public ServiceId ServiceId;
#warning Use TypeSerializationInfo instead?
        public string[] Interfaces;

        public Type GetObjectType() => typeof(ServiceProxyContext);
    }
}
