using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Dasync.Azure.Functions.AspNetCoreMiddleware
{
    internal static class HttpTrigger
    {
        public static Task<IActionResult> Invoke(
            HttpRequest request,
            ExecutionContext context,
            ILogger logger,
            CancellationToken ct)
        {
            FunctionInvocationInput.Current.Value = new FunctionInvocationInput
            {
                Context = context,
                Logger = logger,
                CancellationToken = ct
            };
            return request.ReplyWithMiddlewareAsync();
        }
    }
}
