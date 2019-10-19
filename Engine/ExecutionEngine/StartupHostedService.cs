using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Ioc;
using Dasync.Modeling;
using Microsoft.Extensions.Hosting;

namespace Dasync.ExecutionEngine
{
    public class StartupHostedService : IHostedService
    {
        private readonly ICommunicationModel _communicationModel;
        private readonly IDomainServiceProvider _domainServiceProvider;

        public StartupHostedService(
            ICommunicationModel communicationModel,
            IDomainServiceProvider domainServiceProvider)
        {
            _communicationModel = communicationModel;
            _domainServiceProvider = domainServiceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Resolve all services to make sure that they have proper proxies
            // and allow them to subscribe for events in their constructors.

            foreach (var serviceDefinition in _communicationModel.Services)
            {
                if (serviceDefinition.Implementation != null)
                {
                    _domainServiceProvider.GetService(serviceDefinition.Implementation);
                }

                if (serviceDefinition.Interfaces?.Length > 0)
                {
                    foreach (var interfaceType in serviceDefinition.Interfaces)
                    {
                        _domainServiceProvider.GetService(interfaceType);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // TODO: wait for outstanding transitions to complete
            return Task.CompletedTask;
        }
    }
}
