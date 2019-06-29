using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Platform;

namespace Dasync.Fabric.Sample.Base
{
    internal interface IInternalRoutineCompletionNotifier
    {
        void RegisterComittedRoutine(string routineIntentId, IFabricConnector fabricConnector, ActiveRoutineInfo routineInfo);
    }

    public class RoutineCompletionNotifier : IRoutineCompletionNotifier, IInternalRoutineCompletionNotifier
    {
        private struct FabricConnectorAndRoutineInfo
        {
            public IFabricConnector FabricConnector;
            public ActiveRoutineInfo RoutineInfo;
        }

        private readonly IFabricConnectorSelector _fabricConnectorSelector;
        private readonly Dictionary<string, FabricConnectorAndRoutineInfo> _committedRoutines =
            new Dictionary<string, FabricConnectorAndRoutineInfo>();
        private readonly Dictionary<string, TaskResult> _routineResults
            = new Dictionary<string, TaskResult>();

        public RoutineCompletionNotifier(IFabricConnectorSelector fabricConnectorSelector)
        {
            _fabricConnectorSelector = fabricConnectorSelector;
        }

        public void RegisterComittedRoutine(string routineIntentId, IFabricConnector fabricConnector, ActiveRoutineInfo routineInfo)
        {
            lock (_committedRoutines)
            {
                _committedRoutines.Add(routineIntentId, new FabricConnectorAndRoutineInfo { FabricConnector = fabricConnector, RoutineInfo = routineInfo });
            }
        }

        public Task<TaskResult> TryPollCompletionAsync(
            ServiceId serviceId,
            RoutineMethodId methodId,
            string intentId,
            CancellationToken ct)
        {
            lock (_routineResults)
            {
                _routineResults.TryGetValue(intentId, out var taskResult);
                return Task.FromResult(taskResult);
            }
        }

        public async void NotifyCompletion(
            ServiceId serviceId,
            RoutineMethodId methodId,
            string intentId,
            TaskCompletionSource<TaskResult> completionSink,
            CancellationToken ct)
        {
            FabricConnectorAndRoutineInfo fabricConnectorAndRoutineInfo;

            while (true)
            {
                lock (_committedRoutines)
                {
                    if (_committedRoutines.TryGetValue(intentId, out fabricConnectorAndRoutineInfo))
                    {
                        _committedRoutines.Remove(intentId);
                        break;
                    }
                }
                await Task.Delay(5);
            }

#warning Add infrastructure exception handling

            for (var i = 0; fabricConnectorAndRoutineInfo.RoutineInfo.Result == null; i++)
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

            lock (_routineResults)
                _routineResults.Add(intentId, fabricConnectorAndRoutineInfo.RoutineInfo.Result);

            Task.Run(() =>
                completionSink.SetResult(fabricConnectorAndRoutineInfo.RoutineInfo.Result)
            );
        }
    }
}