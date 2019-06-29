using System;
using Dasync.EETypes.Ioc;

namespace DasyncFeatures
{
    public class DomainServiceProvider : IDomainServiceProvider
    {
#warning temporarily
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
