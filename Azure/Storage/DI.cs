using System;
using System.Collections.Generic;

namespace Dasync.AzureStorage
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(ICloudStorageAccountFactory)] = typeof(CloudStorageAccountFactory),
        };
    }
}
