using System;
using System.Collections.Generic;
using Dasync.EETypes.Platform;
using Dasync.Fabric.Sample.Base;
using Dasync.ServiceRegistry;

namespace Dasync.Fabric.InMemory
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IFabric)] = typeof(InMemoryFabric),
            [typeof(IFabricConnectorFactory)] = typeof(InMemoryFabricConnectorFactory),
            [typeof(IInMemoryFabricSerializerFactoryAdvisor)] = typeof(InMemoryFabricSerializerFactoryAdvisor),
            [typeof(IServiceDiscovery)] = typeof(InMemoryServiceRepository),
            [typeof(IServicePublisher)] = typeof(InMemoryServiceRepository)
        };
    }
}
