using System;
using System.Collections.Generic;
using Dasync.EntityFrameworkCore.Hooks;
using Dasync.EntityFrameworkCore.UnitOfWork;

namespace Dasync.EntityFrameworkCore
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            [typeof(IDbContextEvents)] = typeof(DbContextEvents),
            [typeof(IDbContextModelExtender)] = typeof(UnitOfWorkDbContextModelExtender)
        };
    }
}
