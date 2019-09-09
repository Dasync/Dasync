using Dasync.AspNetCore.DependencyInjection;
using Dasync.Modeling;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncAspNetCore
{
    public static class ApplicationExtensions
    {
        public static IApplicationBuilder UseDasync(this IApplicationBuilder app, ICommunicationModel model)
        {
            app.ApplicationServices.GetService<CommunicationModelProvider.Holder>().Model = model;

            app.UseMiddleware<ScopedServiceProviderMiddleware>();
            app.UseMiddleware<DasyncMiddleware>();

            return app;
        }
    }
}
