using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Fabric;
using Dasync.EETypes.Intents;
using Dasync.ExecutionEngine.Extensions;

namespace Dasync.ExecutionEngine.Proxy
{
    public interface IRoutineImmediateExecutor
    {
        void ExecuteAndAwaitInBackground(ExecuteRoutineIntent intent, Task proxyTask);
    }

    public class RoutineImmediateExecutor : IRoutineImmediateExecutor
    {
        private readonly IFabricConnectorSelector _fabricConnectorSelector;

        public RoutineImmediateExecutor(IFabricConnectorSelector fabricConnectorSelector)
        {
            _fabricConnectorSelector = fabricConnectorSelector;
        }

        /// <remarks>
        /// Fire and forget mode (async void).
        /// </remarks>
        public async void ExecuteAndAwaitInBackground(ExecuteRoutineIntent intent, Task proxyTask)
        {
#warning Add infrastructure exception handling

            try
            {
#warning need to associate a routine with a cancellaion token, and abandon polling when canceled
                var ct = CancellationToken.None;

                var fabricConnector = _fabricConnectorSelector.Select(intent.ServiceId);

                var routineInfo = await fabricConnector.ScheduleRoutineAsync(intent, ct);

                for (var i = 0; ; i++)
                {
#warning Ideally need to handle 'fire an forget' cases - the continuation is never set?
                    //if (!proxyTask.continuation == null after 1 min?)
                    //    return;

                    routineInfo = await fabricConnector.PollRoutineResultAsync(routineInfo, ct);
                    if (routineInfo.Result != null)
                        break;

                    TimeSpan delayInterval;

                    if (routineInfo is IRoutinePollInterval routinePollInterval)
                    {
                        delayInterval = routinePollInterval.Suggest(i);
                    }
                    else
                    {
                        delayInterval = TimeSpan.FromSeconds(0.5);
                    }

                    await Task.Delay(delayInterval);
                }

                if (!proxyTask.TrySetResult(routineInfo.Result))
                    throw new InvalidOperationException("Critical error - cannot set result to the proxy task.");
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}
