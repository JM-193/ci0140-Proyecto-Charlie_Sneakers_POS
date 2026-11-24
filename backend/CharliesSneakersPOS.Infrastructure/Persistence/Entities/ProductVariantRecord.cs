
namespace CharliesSneakersPOS.Infrastructure.Persistence.Entities;

public sealed class ProductVariantRecord
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
