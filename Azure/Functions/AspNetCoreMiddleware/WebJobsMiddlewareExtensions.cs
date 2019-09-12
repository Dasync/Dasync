using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Dasync.Azure.Functions.AspNetCoreMiddleware
{
    internal static class WebJobsMiddlewareExtensions
    {
        public static IServiceCollection AddMiddlewareSupport(this IServiceCollection services)
        {
            services.AddSingleton<RequestDelegateHolder>();
            services.AddSingleton<IHostedService, MiddlewareSupportService>();
            services.EnableRegisteredHostedServices();
            services.AddScoped<IMiddlewareFactory, MiddlewareFactory>();
            services.AddFunctionInvocationInput();
            return services;
        }

        private static IServiceCollection AddFunctionInvocationInput(this IServiceCollection services)
        {
            if (!services.Any(d => d.ServiceType == typeof(ExecutionContext)))
                services.AddScoped(_ => FunctionInvocationInput.Current.Value.Context);

            if (!services.Any(d => d.ServiceType == typeof(ILogger)))
                services.AddScoped(_ => FunctionInvocationInput.Current.Value.Logger);

            if (!services.Any(d => d.ServiceType == typeof(Func<CancellationToken>)))
                services.AddScoped<Func<CancellationToken>>(_ => () => FunctionInvocationInput.Current.Value.CancellationToken);

            return services;
        }
    }
}
