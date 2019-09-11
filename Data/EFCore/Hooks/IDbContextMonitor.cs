using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore.Hooks
{
    public interface IDbContextMonitor
    {
        void OnDbContextCreated(DbContext dbContext);
    }
}
