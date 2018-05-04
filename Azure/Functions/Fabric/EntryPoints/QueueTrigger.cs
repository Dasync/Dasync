using System;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AzureStorage;
using Microsoft.Extensions.Logging;
using FunctionExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Dasync.Fabric.AzureFunctions.EntryPoints
{
    public static class QueueTrigger
    {
        public static async Task RunAsync(
            byte[] content,
            string id,
            string popReceipt,
            int dequeueCount,
            DateTimeOffset insertionTime,
            DateTimeOffset nextVisibleTime,
            DateTimeOffset expirationTime,
            FunctionExecutionContext context,
            ILogger logger,
            CancellationToken ct)
        {
            var messageReceiveTime = DateTimeOffset.Now;

            var runtime = await GlobalStartup.GetRuntimeAsync(context, logger);

            var message = CloudQueueMessageExtensions.Create(
                content, id, popReceipt, dequeueCount, insertionTime, nextVisibleTime, expirationTime);

            await runtime.Fabric.ProcessMessageAsync(message, context, messageReceiveTime, logger, ct);
        }
    }
}