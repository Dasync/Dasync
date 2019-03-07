using System;
using System.Collections.Generic;
using Dasync.AspNetCore.Communication;
using Dasync.AspNetCore.Platform;
using Dasync.EETypes.Ioc;
using Dasync.EETypes.Platform;
using DasyncAspNetCore;

namespace Dasync.AspNetCore
{
    internal static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(ITransitionCommitter)] = typeof(TransitionCommitter),
            [typeof(RoutineCompletionNotifier)] = typeof(RoutineCompletionNotifier),
            [typeof(IRoutineCompletionNotifier)] = typeof(RoutineCompletionNotifier),
            [typeof(IRoutineCompletionSink)] = typeof(RoutineCompletionNotifier),
            [typeof(IEventSubscriber)] = typeof(EventSubscriber),

            [typeof(IPlatformHttpClientProvider)] = typeof(PlatformHttpClientProvider),

            [typeof(IDomainServiceProvider)] = typeof(DomainServiceProvider),

            [typeof(DefaultServiceHttpConfigurator)] = typeof(DefaultServiceHttpConfigurator),
        };
    }
}
