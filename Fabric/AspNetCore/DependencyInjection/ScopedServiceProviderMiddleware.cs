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

        public ScopedServiceProviderMiddleware(IServiceProvider sp) => _serviceProvider = sp;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var previousValue = ScopedServiceProvider.Instance.Value;
            ScopedServiceProvider.Instance.Value = _serviceProvider;
            try
            {
                await next(context);
            }
            finally
            {
                ScopedServiceProvider.Instance.Value = previousValue;
            }
        }
    }
}
