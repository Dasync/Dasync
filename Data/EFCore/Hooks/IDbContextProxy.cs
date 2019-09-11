using System;
using Microsoft.EntityFrameworkCore;

namespace Dasync.EntityFrameworkCore.Hooks
{
    public interface IDbContextProxy
    {
        DbContext Context { get; }

        Action<ModelBuilder> OnModelCreatingCallback { get; set; }
    }
}
