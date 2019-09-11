using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dasync.Azure.Functions.AspNetCoreMiddleware
{
    internal class MiddlewareActionResult : IActionResult
    {
        private readonly RequestDelegate _requestDelegate;
        private readonly ExecutionContext _capturedContext;

        public MiddlewareActionResult(RequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
            _capturedContext = ExecutionContext.Capture();
        }

        public virtual async Task ExecuteResultAsync(ActionContext context)
        {
            Task resultTask = null;
            ExecutionContext.Run(
                _capturedContext,
                _ =>
                {
                    resultTask = _requestDelegate(context.HttpContext);
                },
                null);
            await resultTask;
        }
    }
}
