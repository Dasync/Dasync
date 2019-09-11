using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.Azure.Functions.AspNetCoreMiddleware
{
    public static class HttpRequestExtensions
    {
        public static IActionResult ReplyWithMiddleware(this HttpRequest request) =>
            new MiddlewareActionResult(request.GetMiddlewareRequestDelegate());

        public static Task<IActionResult> ReplyWithMiddlewareAsync(this HttpRequest request) =>
            Task.FromResult(ReplyWithMiddleware(request));

        public static IActionResult ReplyWithMiddleware(this HttpRequest request, Func<RequestDelegate, IActionResult> createActionResult) =>
            createActionResult(request.GetMiddlewareRequestDelegate());

        public static Task<IActionResult> ReplyWithMiddlewareAsync(this HttpRequest request, Func<RequestDelegate, IActionResult> createActionResult) =>
            Task.FromResult(ReplyWithMiddleware(request, createActionResult));

        public static RequestDelegate GetMiddlewareRequestDelegate(this HttpRequest request) =>
            request.HttpContext.RequestServices.GetService<RequestDelegateHolder>().RequestDelegate;
    }
}
