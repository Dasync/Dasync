using Dasync.Fabric.AspNetCore;
using Dasync.Hosting.AspNetCore;
using Dasync.Hosting.AspNetCore.Development;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationExtensions
    {
        public static IApplicationBuilder UseDasync(
            this IApplicationBuilder app,
            bool? isDevelopment = null)
        {
            if (isDevelopment == true || (!isDevelopment.HasValue && AspNetCoreEnvironment.IsDevelopment))
            {
                app.UseMiddleware<EventingMiddleware>();
            }

            app.UseMiddleware<DasyncMiddleware>();
            return app;
        }
    }
}
