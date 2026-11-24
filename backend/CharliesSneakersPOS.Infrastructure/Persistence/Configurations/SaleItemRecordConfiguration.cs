
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharliesSneakersPOS.Infrastructure.Persistence.Configurations;

internal sealed class SaleItemRecordConfiguration
    : IEntityTypeConfiguration<SaleItemRecord>
{
    public void Configure(
        EntityTypeBuilder<SaleItemRecord> builder)
    {
        builder.ToTable("SaleItems", table =>
        {
            table.HasCheckConstraint(
                "CK_SaleItems_Quantity",
                "[Quantity] > 0");

            table.HasCheckConstraint(
                "CK_SaleItems_UnitPrice",
                "[UnitPrice] >= 0");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Sku)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(i => i.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Size)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.Color)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(i => i.Subtotal)
            .HasPrecision(18, 2);

        builder.HasOne<ProductVariantRecord>()
            .WithMany()
            .HasForeignKey(i => i.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.SaleId);
    }
}
