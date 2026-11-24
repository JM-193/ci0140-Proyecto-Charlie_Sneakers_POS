
using CharliesSneakersPOS.Domain.Models;
using CharliesSneakersPOS.Domain.Services;

namespace CharliesSneakersPOS.Domain.Entities;

public sealed class Sale
{
    private readonly List<SaleItem> _items = new();
    private readonly SaleCalculator _calculator = new();

    public IReadOnlyList<SaleItem> Items => _items.AsReadOnly();

    public SaleTotals Totals => _calculator.Calculate(_items);

    // Agregar un producto al carrito
    public void AddItem(ProductVariant product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");

        var existingItem = _items.Find(
            item => item.Product.Id == product.Id
        );

        if (existingItem is null)
        {
            _items.Add(new SaleItem(product, quantity));
            return;
        }

        // Validar la cantidad acumulada
        if (quantity > existingItem.Product.Stock - existingItem.Quantity)
            throw new InvalidOperationException(
                "Insufficient stock for this product.");

        existingItem.ChangeQuantity(
            existingItem.Quantity + quantity
        );
    }

    // Modificar la cantidad de un producto
    public void ChangeQuantity(Guid productId, int quantity)
    {
        var item = FindItem(productId);
        item.ChangeQuantity(quantity);
    }

    // Eliminar un producto del carrito
    public void RemoveItem(Guid productId)
    {
        var item = FindItem(productId);
        _items.Remove(item);
    }

    private SaleItem FindItem(Guid productId)
    {
        return _items.Find(item => item.Product.Id == productId)
            ?? throw new KeyNotFoundException(
                "Product is not in the sale.");
    }
}
