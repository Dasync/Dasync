using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore.Hooks
{
    public class DbContextEvents : IDbContextEvents
    {
        private readonly IEnumerable<IDbContextModelExtender> _modelExtenders;
        private readonly IEnumerable<IDbContextMonitor> _monitors;

        public DbContextEvents(
            IEnumerable<IDbContextModelExtender> modelExtenders,
            IEnumerable<IDbContextMonitor> monitors)
        {
            _modelExtenders = modelExtenders;
            _monitors = monitors;
        }

        public void OnModelCreating(DbContext dbContext, ModelBuilder modelBuilder)
        {
            foreach (var extender in _modelExtenders)
                extender.Extend(dbContext, modelBuilder);
        }

        public void OnContextCreated(DbContext dbContext)
        {
            foreach (var monitor in _monitors)
                monitor.OnDbContextCreated(dbContext);
        }
    }
}
