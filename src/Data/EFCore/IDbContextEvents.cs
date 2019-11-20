using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore
{
    public interface IDbContextEvents
    {
        void OnContextCreated(DbContext context);

        void OnModelCreating(DbContext dbContext, ModelBuilder modelBuilder);
    }
}
