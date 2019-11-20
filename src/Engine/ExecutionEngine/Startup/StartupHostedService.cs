using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Ioc;
using Dasync.Modeling;
using Microsoft.Extensions.Hosting;

namespace Dasync.ExecutionEngine.Startup
{
    public class StartupHostedService : IHostedService
    {
        private readonly ICommunicationModel _communicationModel;
        private readonly IDomainServiceProvider _domainServiceProvider;
        private readonly ICommunicationListener _communicationListener;

        public StartupHostedService(
            ICommunicationModel communicationModel,
            IDomainServiceProvider domainServiceProvider,
            ICommunicationListener communicationListener)
        {
            _communicationModel = communicationModel;
            _domainServiceProvider = domainServiceProvider;
            _communicationListener = communicationListener;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ResolveAllDomainServices();

            await _communicationListener.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _communicationListener.StopAsync(cancellationToken);

            // TODO: wait for outstanding transitions to complete
        }

        private void ResolveAllDomainServices()
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
        }
    }
}
