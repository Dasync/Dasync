using Dasync.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore.UnitOfWork
{
    public class CurrentDbContextProvider<TContext> : ICurrentDbContext<TContext> where TContext : DbContext
    {
        private readonly IScopedServiceProvider _serviceProvider;

        public CurrentDbContextProvider(IScopedServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TContext Instance => (TContext)_serviceProvider.GetService(typeof(TContext));
    }
}
