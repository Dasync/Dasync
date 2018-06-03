using System;
using System.Collections.Generic;
using Dasync.EETypes.Fabric;
using Dasync.ServiceRegistry;

namespace Dasync.Fabric.FileBased
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IFabric)] = typeof(FileBasedFabric),
            [typeof(IFabricConnectorFactory)] = typeof(FileBasedFabricConnectorFactory),
            [typeof(IFileBasedFabricSerializerFactoryAdvisor)] = typeof(FileBasedFabricSerializerFactoryAdvisor),
            [typeof(IServiceDiscovery)] = typeof(FileBasedServiceRepository),
            [typeof(IServicePublisher)] = typeof(FileBasedServiceRepository)
        };
    }
}
