using Dasync.EntityFrameworkCore.Hooks;
using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore.UnitOfWork
{
    public class UnitOfWorkDbContextModelExtender : IDbContextModelExtender
    {
        public void Extend(DbContext dbContext, ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UnitOfWorkRecordConfiguration());
        }
    }
}
