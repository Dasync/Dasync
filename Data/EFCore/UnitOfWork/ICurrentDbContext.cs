using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore.UnitOfWork
{
    public interface ICurrentDbContext<TContext> where TContext : DbContext
    {
        TContext Instance { get; }
    }
}
