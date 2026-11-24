
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharliesSneakersPOS.Infrastructure.Persistence.Configurations;

internal sealed class SalePaymentRecordConfiguration
    : IEntityTypeConfiguration<SalePaymentRecord>
{
    public void Configure(
        EntityTypeBuilder<SalePaymentRecord> builder)
    {
        builder.ToTable("SalePayments", table =>
        {
            table.HasCheckConstraint(
                "CK_SalePayments_Amount",
                "[Amount] > 0");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.HasIndex(p => p.SaleId);
    }
}
