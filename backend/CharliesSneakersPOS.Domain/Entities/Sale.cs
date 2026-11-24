
using CharliesSneakersPOS.Domain.Models;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Domain.Services;

namespace CharliesSneakersPOS.Domain.Entities;

public sealed class Sale
{
    private readonly List<SaleItem> _items = new();
    private readonly List<Payment> _payments = new();

    private readonly SaleCalculator _calculator = new();
    private readonly PaymentCalculator _paymentCalculator = new();

    public IReadOnlyList<SaleItem> Items => _items.AsReadOnly();
    public IReadOnlyList<Payment> Payments => _payments.AsReadOnly();

    public SaleTotals Totals => _calculator.Calculate(_items);

    public bool IsCompleted { get; private set; }

    public PaymentSummary? PaymentSummary { get; private set; }

    // Agregar productos al carrito.
    public void AddItem(ProductVariant product, int quantity)
    {
        EnsureNotCompleted();
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

        if (quantity > existingItem.Product.Stock - existingItem.Quantity)
            throw new InvalidOperationException(
                "Insufficient stock for this product.");

        existingItem.ChangeQuantity(
            existingItem.Quantity + quantity
        );
    }

    // Modificar la cantidad.
    public void ChangeQuantity(Guid productId, int quantity)
    {
        EnsureNotCompleted();

        var item = FindItem(productId);
        item.ChangeQuantity(quantity);
    }

    // Eliminar un producto.
    public void RemoveItem(Guid productId)
    {
        EnsureNotCompleted();

        var item = FindItem(productId);
        _items.Remove(item);
    }

    // Consultar pagos sin finalizar la venta.
    public PaymentSummary PreviewPayments(
        IEnumerable<Payment> payments)
    {
        EnsureNotCompleted();

        if (_items.Count == 0 || Totals.Total <= 0m)
            throw new InvalidOperationException(
                "A sale must have a positive total.");

        return _paymentCalculator.Calculate(
            Totals.Total,
            payments
        );
    }

    // Completar la venta después de validar los pagos.
    public void Complete(IEnumerable<Payment> payments)
    {
        EnsureNotCompleted();
        ArgumentNullException.ThrowIfNull(payments);

        var paymentList = payments.ToList();

        var summary = PreviewPayments(paymentList);

        if (!summary.IsFullyPaid)
            throw new InvalidOperationException(
                "The sale has an outstanding balance.");

        _payments.AddRange(paymentList);

        foreach (var item in _items)
        {
            item.Lock();
        }

        PaymentSummary = summary;
        IsCompleted = true;
    }

    private SaleItem FindItem(Guid productId)
    {
        return _items.Find(
            item => item.Product.Id == productId
        ) ?? throw new KeyNotFoundException(
            "Product is not in the sale.");
    }

    private void EnsureNotCompleted()
    {
        if (IsCompleted)
            throw new InvalidOperationException(
                "The sale has already been completed.");
    }
}
