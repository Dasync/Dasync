using System;

namespace Dasync.DependencyInjection
{
    public interface IServiceProviderScope
    {
        ServiceProviderScope New();

        ServiceProviderScope Register(IServiceProvider scopedServiceProvider);
    }
}
