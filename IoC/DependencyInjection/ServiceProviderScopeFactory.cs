using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.DependencyInjection
{
    public class ServiceProviderScopeFactory : IServiceProviderScope
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ServiceProviderScopeFactory(IServiceScopeFactory serviceScopeFactory) =>
            _serviceScopeFactory = serviceScopeFactory;

        public ServiceProviderScope New() =>
            ServiceProviderScope.Enter(_serviceScopeFactory.CreateScope());

        public ServiceProviderScope Register(IServiceProvider scopedServiceProvider) =>
            ServiceProviderScope.Enter(scopedServiceProvider);
    }
}
