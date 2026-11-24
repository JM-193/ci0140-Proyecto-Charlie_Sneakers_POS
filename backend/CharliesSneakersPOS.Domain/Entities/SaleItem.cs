
namespace CharliesSneakersPOS.Domain.Entities;

public sealed class SaleItem
{
    public ProductVariant Product { get; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; }

    public decimal Subtotal => UnitPrice * Quantity;

    public SaleItem(ProductVariant product, int quantity)
    {
        Product = product ?? throw new ArgumentNullException(nameof(product));
        UnitPrice = product.UnitPrice;

        ChangeQuantity(quantity);
    }

    public void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");

        if (!Product.HasSufficientStock(quantity))
            throw new InvalidOperationException(
                "Insufficient stock for this product.");

        Quantity = quantity;
    }
}
