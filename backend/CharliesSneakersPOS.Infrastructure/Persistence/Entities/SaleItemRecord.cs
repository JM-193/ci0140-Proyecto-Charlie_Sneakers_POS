
namespace CharliesSneakersPOS.Infrastructure.Persistence.Entities;

public sealed class SaleItemRecord
{
    public Guid Id { get; set; }

    public Guid SaleId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }
}
