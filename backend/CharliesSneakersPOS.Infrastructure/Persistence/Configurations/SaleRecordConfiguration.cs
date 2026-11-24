
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharliesSneakersPOS.Infrastructure.Persistence.Configurations;

internal sealed class SaleRecordConfiguration
    : IEntityTypeConfiguration<SaleRecord>
{
    public void Configure(EntityTypeBuilder<SaleRecord> builder)
    {
        builder.ToTable("Sales", table =>
        {
            table.HasCheckConstraint(
                "CK_Sales_Total",
                "[Total] > 0");

            table.HasCheckConstraint(
                "CK_Sales_Change",
                "[Change] >= 0");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.CreatedAt).IsRequired();

        builder.Property(s => s.Subtotal).HasPrecision(18, 2);
        builder.Property(s => s.Tax).HasPrecision(18, 2);
        builder.Property(s => s.Total).HasPrecision(18, 2);
        builder.Property(s => s.Change).HasPrecision(18, 2);

        builder.HasOne<CashSession>()
            .WithMany()
            .HasForeignKey(s => s.CashSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.CashSessionId);

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Payments)
            .WithOne()
            .HasForeignKey(p => p.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
