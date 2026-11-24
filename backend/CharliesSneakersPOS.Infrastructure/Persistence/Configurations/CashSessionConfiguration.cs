using CharliesSneakersPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharliesSneakersPOS.Infrastructure.Persistence.Configurations;

internal sealed class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.ToTable("CashSessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.OpeningAmount).HasPrecision(18, 2);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
        builder.Ignore(s => s.IsOpen);

        // Garantiza a nivel de BD que solo exista una sesión abierta.
        builder.HasIndex(s => s.Status)
            .IsUnique()
            .HasFilter("[Status] = 'Open'");
    }
}
