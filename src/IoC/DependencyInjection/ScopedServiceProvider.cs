using System;
using System.Threading;

namespace Dasync.DependencyInjection
{
    public class ScopedServiceProvider : IScopedServiceProvider
    {
        public static readonly AsyncLocal<IServiceProvider> Instance = new AsyncLocal<IServiceProvider>();

        public object GetService(Type serviceType)
        {
            var sp = Instance.Value;
            if (sp == null)
                throw new InvalidOperationException("A service resolution has been requested outside of a scope.");
            return sp.GetService(serviceType);
        }
    }
}
