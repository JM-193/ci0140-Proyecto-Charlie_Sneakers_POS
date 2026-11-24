
namespace CharliesSneakersPOS.Domain.Entities;

public sealed class ProductVariant
{
    public Guid Id { get; }
    public string Sku { get; }
    public string Name { get; }
    public string Size { get; }
    public string Color { get; }
    public decimal UnitPrice { get; }
    public int Stock { get; private set; }

    public ProductVariant(
        Guid id,
        string sku,
        string name,
        string size,
        string color,
        decimal unitPrice,
        int stock)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Invalid product ID.");

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.");

        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);
        ArgumentOutOfRangeException.ThrowIfNegative(stock);

        Id = id;
        Sku = sku;
        Name = name;
        Size = size;
        Color = color;
        UnitPrice = unitPrice;
        Stock = stock;
    }

    public bool HasSufficientStock(int quantity)
    {
        return quantity > 0 && Stock >= quantity;
    }
}
