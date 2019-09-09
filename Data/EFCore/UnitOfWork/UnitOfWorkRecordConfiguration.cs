using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dasync.EntityFrameworkCore.UnitOfWork
{
    public class UnitOfWorkRecordConfiguration : IEntityTypeConfiguration<UnitOfWorkRecord>
    {
        public void Configure(EntityTypeBuilder<UnitOfWorkRecord> builder)
        {
            builder.ToTable("uow");
        }
    }
}
