using System;
using System.Collections.Generic;
using Dasync.EETypes.Fabric;
using Dasync.FabricConnector.AzureStorage;
using Dasync.Ioc;
using Dasync.ServiceRegistry;

namespace Dasync.Fabric.AzureFunctions
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IFabric)] = typeof(AzureFunctionsFabric),
            [typeof(IAzureWebJobsEnviromentalSettings)] = typeof(AzureWebJobsEnviromentalSettings),
            [typeof(AzureFunctionsFabricSettings)] = typeof(AzureFunctionsFabricSettings),
            [typeof(IStorageAccontConnectionStringResolver)] = typeof(StorageAccontConnectionStringResolver),

            [typeof(IServiceDiscovery)] = typeof(AzureStorageServiceRepository),
            [typeof(IServicePublisher)] = typeof(AzureStorageServiceRepository),

            [typeof(ContainerConverters)] = typeof(ContainerConverters),
        };
    }
}
