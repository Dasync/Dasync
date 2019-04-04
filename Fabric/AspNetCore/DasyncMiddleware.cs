using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DasyncAspNetCore
{
    public class DasyncMiddleware : IMiddleware
    {
        private readonly DasyncOptions _options;
        private readonly IHttpRequestHandler _httpRequestHandler;

        public DasyncMiddleware(IOptionsMonitor<DasyncOptions> optionsMonitor, IHttpRequestHandler httpRequestHandler)
        {
            _options = optionsMonitor.CurrentValue;
            _httpRequestHandler = httpRequestHandler;

            if (_options.ApiPath == null)
                _options.ApiPath = DasyncOptions.Defaults.ApiPath;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!context.Request.Path.StartsWithSegments(_options.ApiPath))
            {
                await next(context);
                return;
            }

            await _httpRequestHandler.HandleAsync(_options.ApiPath, context, context.RequestAborted);
        }
    }

    public class DasyncOptions
    {
        public string ApiPath { get; set; }

        public static readonly DasyncOptions Defaults =
            new DasyncOptions
            {
                ApiPath = "/api"
            };
    }
}
