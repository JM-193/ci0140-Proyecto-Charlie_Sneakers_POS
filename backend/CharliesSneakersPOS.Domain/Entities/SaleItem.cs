
namespace CharliesSneakersPOS.Domain.Entities;

public sealed class SaleItem
{
    private bool _isLocked;

    public ProductVariant Product { get; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; }

    public decimal Subtotal => UnitPrice * Quantity;

    public SaleItem(ProductVariant product, int quantity)
    {
        Product = product
            ?? throw new ArgumentNullException(nameof(product));

        UnitPrice = product.UnitPrice;

        ChangeQuantity(quantity);
    }

    public void ChangeQuantity(int quantity)
    {
        if (_isLocked)
            throw new InvalidOperationException(
                "Completed sale items cannot be modified.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");

        if (!Product.HasSufficientStock(quantity))
            throw new InvalidOperationException(
                "Insufficient stock for this product.");

        Quantity = quantity;
    }

    internal void Lock()
    {
        _isLocked = true;
    }
}
