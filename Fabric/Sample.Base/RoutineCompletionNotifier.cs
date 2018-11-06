using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Platform;

namespace Dasync.Fabric.Sample.Base
{
    internal interface IInternalRoutineCompletionNotifier
    {
        void RegisterComittedRoutine(long routineIntentId, IFabricConnector fabricConnector, ActiveRoutineInfo routineInfo);
    }

    public class RoutineCompletionNotifier : IRoutineCompletionNotifier, IInternalRoutineCompletionNotifier
    {
        private struct FabricConnectorAndRoutineInfo
        {
            public IFabricConnector FabricConnector;
            public ActiveRoutineInfo RoutineInfo;
        }

        private readonly IFabricConnectorSelector _fabricConnectorSelector;
        private readonly Dictionary<long, FabricConnectorAndRoutineInfo> _committedRoutines =
            new Dictionary<long, FabricConnectorAndRoutineInfo>();

        public RoutineCompletionNotifier(IFabricConnectorSelector fabricConnectorSelector)
        {
            _fabricConnectorSelector = fabricConnectorSelector;
        }

        public void RegisterComittedRoutine(long routineIntentId, IFabricConnector fabricConnector, ActiveRoutineInfo routineInfo)
        {
            lock (_committedRoutines)
            {
                _committedRoutines.Add(routineIntentId, new FabricConnectorAndRoutineInfo { FabricConnector = fabricConnector, RoutineInfo = routineInfo });
            }
        }

        public async void NotifyCompletion(long routineIntentId, TaskCompletionSource<TaskResult> completionSink)
        {
            FabricConnectorAndRoutineInfo fabricConnectorAndRoutineInfo;

            lock (_committedRoutines)
            {
                fabricConnectorAndRoutineInfo = _committedRoutines[routineIntentId];
                _committedRoutines.Remove(routineIntentId);
            }

#warning Add infrastructure exception handling

#warning need to associate a routine with a cancellaion token, and abandon polling when canceled?
            var ct = CancellationToken.None;

            for (var i = 0; ; i++)
            {
#warning Ideally need to handle 'fire an forget' cases - the continuation is never set?
                //if (!proxyTask.continuation == null after 1 min?)
                //    return;

                fabricConnectorAndRoutineInfo.RoutineInfo = await fabricConnectorAndRoutineInfo.FabricConnector.PollRoutineResultAsync(fabricConnectorAndRoutineInfo.RoutineInfo, ct);
                if (fabricConnectorAndRoutineInfo.RoutineInfo.Result != null)
                    break;

                TimeSpan delayInterval;

                if (fabricConnectorAndRoutineInfo.RoutineInfo is IRoutinePollInterval routinePollInterval)
                {
                    delayInterval = routinePollInterval.Suggest(i);
                }
                else
                {
                    delayInterval = TimeSpan.FromSeconds(0.5);
                }

                await Task.Delay(delayInterval);
            }

            Task.Run(() =>
                completionSink.SetResult(fabricConnectorAndRoutineInfo.RoutineInfo.Result)
            );
        }
    }
}