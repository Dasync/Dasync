using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore
{
    public class DasyncDbContext : DbContext
    {
        private readonly IDbContextEvents _dbContextEvents;

        protected DasyncDbContext() : base() { }

        public DasyncDbContext(DbContextOptions options, IDbContextEvents dbContextEvents) : base(options)
        {
            _dbContextEvents = dbContextEvents;
            dbContextEvents?.OnContextCreated(this);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            _dbContextEvents?.OnModelCreating(this, modelBuilder);
        }
    }
}
