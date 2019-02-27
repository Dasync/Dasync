using System;
using System.Collections.Generic;

namespace Dasync.Modeling
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(ICommunicationModelProvider)] = typeof(CommunicationModelProvider),
            [typeof(CommunicationModelProvider.Holder)] = typeof(CommunicationModelProvider.Holder)
        };
    }
}
