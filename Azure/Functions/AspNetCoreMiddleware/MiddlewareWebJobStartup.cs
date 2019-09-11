using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(Dasync.Azure.Functions.AspNetCoreMiddleware.MiddlewareWebJobStartup))]

namespace Dasync.Azure.Functions.AspNetCoreMiddleware
{
    internal class MiddlewareWebJobStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddMiddlewareSupport();
        }
    }
}
