using System;
using System.Collections.Generic;
using Dasync.Serializers.EETypes.Cancellation;
using Dasync.Serializers.EETypes.Triggers;

namespace Dasync.Serializers.EETypes
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(EETypesNameShortener)] = typeof(EETypesNameShortener),
            [typeof(EEAssemblyNameShortener)] = typeof(EEAssemblyNameShortener),
            [typeof(EETypesSerializerSelector)] = typeof(EETypesSerializerSelector),
            [typeof(ServiceProxySerializer)] = typeof(ServiceProxySerializer),
            [typeof(CancellationTokenSourceSerializer)] = typeof(CancellationTokenSourceSerializer),
            [typeof(TaskCompletionSourceSerializer)] = typeof(TaskCompletionSourceSerializer),
        };
    }
}
