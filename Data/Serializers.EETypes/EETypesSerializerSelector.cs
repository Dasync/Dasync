using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Accessors;
using Dasync.EETypes.Proxy;
using Dasync.Proxy;
using Dasync.Serialization;
using Dasync.Serializers.EETypes.Cancellation;

namespace Dasync.Serializers.EETypes
{
    public sealed class EETypesSerializerSelector : IObjectDecomposerSelector, IObjectComposerSelector
    {
        private static readonly TaskAwaiterSerializer _taskAwaiterSerializer = new TaskAwaiterSerializer();
        private static readonly TaskSerializer _taskSerializer = new TaskSerializer();
        private static readonly CancellationTokenSerializer _cancellationTokenSerializer = new CancellationTokenSerializer();

        private readonly ServiceProxySerializer _serviceProxySerializer;
        private readonly CancellationTokenSourceSerializer _cancellationTokenSourceSerializer;

        public EETypesSerializerSelector(
            ServiceProxySerializer serviceProxySerializer,
            CancellationTokenSourceSerializer cancellationTokenSourceSerializer)
        {
            _serviceProxySerializer = serviceProxySerializer;
            _cancellationTokenSourceSerializer = cancellationTokenSourceSerializer;
        }

        public IObjectDecomposer SelectDecomposer(Type valueType)
        {
            if (TaskAwaiterUtils.IsAwaiterType(valueType))
                return _taskAwaiterSerializer;

            if (typeof(Task).IsAssignableFrom(valueType))
                return _taskSerializer;

            if (typeof(CancellationTokenSource).IsAssignableFrom(valueType))
                return _cancellationTokenSourceSerializer;

            if (valueType == typeof(CancellationToken))
                return _cancellationTokenSerializer;

            if (typeof(IProxy).IsAssignableFrom(valueType))
                return _serviceProxySerializer;

            return null;
        }

        public IObjectComposer SelectComposer(Type targetType)
        {
            if (TaskAwaiterUtils.IsAwaiterType(targetType))
                return _taskAwaiterSerializer;

            if (typeof(Task).IsAssignableFrom(targetType))
                return _taskSerializer;

            if (typeof(ServiceProxyContext).IsAssignableFrom(targetType))
                return _serviceProxySerializer;

            if (typeof(CancellationTokenSource).IsAssignableFrom(targetType))
                return _cancellationTokenSourceSerializer;

            if (targetType == typeof(CancellationToken))
                return _cancellationTokenSerializer;

            return null;
        }
    }
}
