using System;
using System.Collections.Generic;

namespace Dasync.Serialization.Json
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(ISerializerFactory)] = typeof(DasyncJsonSerializerFactory),
        };
    }
}
