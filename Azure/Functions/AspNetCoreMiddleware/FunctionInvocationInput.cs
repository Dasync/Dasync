using System.Threading;
using Microsoft.Extensions.Logging;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Dasync.Azure.Functions.AspNetCoreMiddleware
{
    internal sealed class FunctionInvocationInput
    {
        public static AsyncLocal<FunctionInvocationInput> Current = new AsyncLocal<FunctionInvocationInput>();

        public ExecutionContext Context { get; set; }

        public ILogger Logger { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
