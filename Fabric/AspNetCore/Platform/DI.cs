using System;
using System.Collections.Generic;
using Dasync.EETypes.Platform;

namespace Dasync.AspNetCore.Platform
{
    internal static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(ITransitionCommitter)] = typeof(TransitionCommitter),
            [typeof(RoutineCompletionNotifier)] = typeof(RoutineCompletionNotifier),
            [typeof(IRoutineCompletionNotifier)] = typeof(RoutineCompletionNotifier),
            [typeof(IRoutineCompletionSink)] = typeof(RoutineCompletionNotifier),
            [typeof(EventDispatcher)] = typeof(EventDispatcher),
            [typeof(IEventDispatcher)] = typeof(EventDispatcher),
            [typeof(IEventSubscriber)] = typeof(EventDispatcher),
        };
    }
}
