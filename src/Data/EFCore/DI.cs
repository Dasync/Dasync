using System;
using System.Collections.Generic;
using Dasync.EntityFrameworkCore.Hooks;
using Dasync.EntityFrameworkCore.Serialization;
using Dasync.EntityFrameworkCore.UnitOfWork;
using Dasync.Serialization;

namespace Dasync.EntityFrameworkCore
{
    public static class DI
    {
        public static readonly Dictionary<Type, Type> Bindings = new Dictionary<Type, Type>
        {
            // Hooks
            [typeof(IDbContextEvents)] = typeof(DbContextEvents),

            // Unit of Work
            [typeof(IDbContextModelExtender)] = typeof(UnitOfWorkDbContextModelExtender),

            // Serialization
            [typeof(IKnownDbContextSet)] = typeof(KnownDbContextSet),
            [typeof(SerializerSelector)] = typeof(SerializerSelector),
            [typeof(IObjectDecomposerSelector)] = typeof(SerializerSelector),
            [typeof(IObjectComposerSelector)] = typeof(SerializerSelector),
            [typeof(EntitySerializer)] = typeof(EntitySerializer),
            [typeof(EntityProjectionSerializer)] = typeof(EntityProjectionSerializer),
            [typeof(DbContextSerializer)] = typeof(DbContextSerializer),
        };
    }
}
