using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FunctionExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Dasync.Fabric.AzureFunctions.EntryPoints
{
    public static class HttpTrigger
    {
        public static async Task<HttpResponseMessage> RunAsync(
            HttpRequestMessage request,
            FunctionExecutionContext context,
            ILogger logger,
            CancellationToken ct)
        {
            var requestStartTime = DateTimeOffset.Now;
            var runtime = await GlobalStartup.GetRuntimeAsync(context, logger);
            return await runtime.Fabric.ProcessRequestAsync(request, context, requestStartTime, logger, ct);
        }
    }
}
