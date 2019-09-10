using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.DependencyInjection
{
    public struct ServiceProviderScope : IDisposable
    {
        private IServiceScope _serviceScope;
        private IServiceProvider _prevServiceProvider;

        public static ServiceProviderScope Enter(IServiceScope serviceScope)
        {
            var scope = new ServiceProviderScope
            {
                _serviceScope = serviceScope,
                _prevServiceProvider = ScopedServiceProvider.Instance.Value
            };
            ScopedServiceProvider.Instance.Value = serviceScope.ServiceProvider;
            return scope;
        }

        public static ServiceProviderScope Enter(IServiceProvider serviceProvider)
        {
            var scope = new ServiceProviderScope
            {
                _prevServiceProvider = ScopedServiceProvider.Instance.Value
            };
            ScopedServiceProvider.Instance.Value = serviceProvider;
            return scope;
        }

        public void Dispose()
        {
            ScopedServiceProvider.Instance.Value = _prevServiceProvider;
            _prevServiceProvider = null;
            _serviceScope?.Dispose();
            _serviceScope = null;
        }
    }
}
