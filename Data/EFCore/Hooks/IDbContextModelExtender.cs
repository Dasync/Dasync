using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore.Hooks
{
    public interface IDbContextModelExtender
    {
        void Extend(DbContext dbContext, ModelBuilder modelBuilder);
    }
}
