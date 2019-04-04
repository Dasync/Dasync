using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace DasyncAspNetCore
{
    internal class DasyncStartupFilter : IStartupFilter
    {
        private readonly IApplicationLifetime _appLifetime;

        public DasyncStartupFilter(IApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime;

            appLifetime.ApplicationStarted.Register(OnAppStarted, useSynchronizationContext: false);
        }

        private void OnAppStarted()
        {
            return;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return next;
        }
    }
}
