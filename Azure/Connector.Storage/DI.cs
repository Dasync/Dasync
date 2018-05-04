using System;
using System.Collections.Generic;
using Dasync.EETypes.Fabric;

namespace Dasync.FabricConnector.AzureStorage
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IFabricConnectorFactory)] = typeof(AzureStorageFabricConnectorFactory),
        };
    }
}
