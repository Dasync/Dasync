using System;
using Dasync.EETypes.Ioc;

namespace Dasync.ExecutionEngine
{
    public class DomainServiceProvider : IDomainServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public DomainServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }
    }
}
