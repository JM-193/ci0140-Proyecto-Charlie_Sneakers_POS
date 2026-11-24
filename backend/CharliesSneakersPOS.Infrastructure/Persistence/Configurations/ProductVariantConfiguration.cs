
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharliesSneakersPOS.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantConfiguration
    : IEntityTypeConfiguration<ProductVariantRecord>
{
    public void Configure(
        EntityTypeBuilder<ProductVariantRecord> builder)
    {
        builder.ToTable("ProductVariants", table =>
        {
            table.HasCheckConstraint(
                "CK_ProductVariants_Stock",
                "[Stock] >= 0");

            table.HasCheckConstraint(
                "CK_ProductVariants_Price",
                "[UnitPrice] >= 0");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Sku)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Barcode)
            .HasMaxLength(64);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Size)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.Color)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(p => p.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.RowVersion)
            .IsRowVersion();
    }
}
