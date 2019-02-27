using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DasyncAspNetCore
{
    public class DasyncCoHost : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            return;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            return;
        }
    }
}
