using Dasync.AspNetCore.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace DasyncAspNetCore
{
    public static class ApplicationExtensions
    {
        public static IApplicationBuilder UseDasync(this IApplicationBuilder app)
        {
            app.UseMiddleware<ScopedServiceProviderMiddleware>();
            app.UseMiddleware<DasyncMiddleware>();
            return app;
        }
    }
}
