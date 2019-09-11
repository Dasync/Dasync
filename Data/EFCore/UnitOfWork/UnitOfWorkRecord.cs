using System;

namespace Dasync.EntityFrameworkCore.UnitOfWork
{
    public class UnitOfWorkRecord
    {
        public string Id { get; set; }

        public DateTimeOffset Time { get; set; }
    }
}
