using System;
using System.Collections.Generic;

namespace Dasync.AsyncStateMachine
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IAsyncStateMachineAccessorFactory)] = typeof(AsyncStateMachineAccessorFactory),
            [typeof(IAsyncStateMachineMetadataBuilder)] = typeof(AsyncStateMachineMetadataBuilder),
            [typeof(IAsyncStateMachineMetadataProvider)] = typeof(AsyncStateMachineMetadataProvider)
        };
    }
}
