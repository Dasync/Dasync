using System;
using System.Collections.Generic;
using Dasync.Fabric.Sample.Base;
using Dasync.ServiceRegistry;

namespace Dasync.Fabric.FileBased
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(FileBasedFabric)] = typeof(FileBasedFabric),
            [typeof(IFabric)] = typeof(FileBasedFabric),
            [typeof(IFabricConnectorFactory)] = typeof(FileBasedFabricConnectorFactory),
            [typeof(IFileBasedFabricSerializerFactoryAdvisor)] = typeof(FileBasedFabricSerializerFactoryAdvisor),
            [typeof(IServiceDiscovery)] = typeof(FileBasedServiceRepository),
            [typeof(IServicePublisher)] = typeof(FileBasedServiceRepository)
        };
    }
}
