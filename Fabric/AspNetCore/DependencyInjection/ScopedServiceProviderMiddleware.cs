using System;
using System.Threading.Tasks;
using Dasync.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace Dasync.AspNetCore.DependencyInjection
{
    /// <summary>
    /// The sole purpose of this middleware is to share a reference to the service provider within a request scope.
    /// This allows singletons to dynamically resolve scoped services.
    /// </summary>
    public class ScopedServiceProviderMiddleware : IMiddleware
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceProviderScope _serviceProviderScope;

        public ScopedServiceProviderMiddleware(IServiceProvider serviceProvider, IServiceProviderScope serviceProviderScope)
        {
            _serviceProvider = serviceProvider;
            _serviceProviderScope = serviceProviderScope;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            using (_serviceProviderScope.Register(_serviceProvider))
            {
                await next(context);
            }
        }
    }
}
