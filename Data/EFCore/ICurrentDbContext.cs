using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore
{
    public interface ICurrentDbContext<TContext> where TContext : DbContext
    {
        TContext Instance { get; }
    }
}
