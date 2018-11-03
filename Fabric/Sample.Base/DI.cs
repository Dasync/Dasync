using System;
using System.Collections.Generic;
using Dasync.EETypes.Platform;

namespace Dasync.Fabric.Sample.Base
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IFabricConnectorFactorySelector)] = typeof(FabricConnectorFactorySelector),
            [typeof(IFabricConnectorSelector)] = typeof(FabricConnectorSelector),
            [typeof(ICurrentFabric)] = typeof(CurrentFabricHolder),
            [typeof(ITransitionCommitter)] = typeof(TransitionCommitter),
            [typeof(IRoutineCompletionNotifier)] = typeof(RoutineCompletionNotifier),
            [typeof(IEventSubscriber)] = typeof(EventSubscriber)
        };
    }
}
