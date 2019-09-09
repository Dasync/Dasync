using Dasync.EntityFrameworkCore.Hooks;
using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore.UnitOfWork
{
    public class UnitOfWorkDbContextDecorator : IDbContextDecorator
    {
        public void Decorate(IDbContextProxy dbContextProxy)
        {
            dbContextProxy.OnModelCreatingCallback += OnModelCreating;
        }

        private void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UnitOfWorkRecordConfiguration());
        }
    }
}
