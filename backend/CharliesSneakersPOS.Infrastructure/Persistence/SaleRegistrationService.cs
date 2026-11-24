
using System.Data;
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CharliesSneakersPOS.Infrastructure.Persistence;

public sealed class SaleRegistrationService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public SaleRegistrationService(
        AppDbContext db,
        TimeProvider? timeProvider = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Guid> RegisterAsync(
        Guid saleId,
        Guid cashSessionId,
        Sale sale,
        IEnumerable<Payment> payments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(payments);

        if (saleId == Guid.Empty)
            throw new ArgumentException(
                "A valid sale ID is required.",
                nameof(saleId));

        if (cashSessionId == Guid.Empty)
            throw new ArgumentException(
                "A valid cash session ID is required.",
                nameof(cashSessionId));

        if (sale.IsCompleted)
            throw new InvalidOperationException(
                "The sale has already been completed.");

        var paymentList = payments.ToList();
        var summary = sale.PreviewPayments(paymentList);

        if (!summary.IsFullyPaid)
            throw new InvalidOperationException(
                "The sale has an outstanding balance.");

        // Caja, ventas e inventario se verifican
        // dentro de una misma transacción.
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var isCashOpen = await _db.CashSessions.AnyAsync(
            session =>
                session.Id == cashSessionId &&
                session.Status == CashSessionStatus.Open,
            cancellationToken);

        if (!isCashOpen)
            throw new InvalidOperationException(
                "The cash register must be open.");

        var saleExists = await _db.Sales.AnyAsync(
            s => s.Id == saleId,
            cancellationToken);

        if (saleExists)
            throw new InvalidOperationException(
                "The sale has already been registered.");

        var itemRecords = new List<SaleItemRecord>();

        foreach (var item in sale.Items)
        {
            var product = await _db.ProductVariants
                .SingleOrDefaultAsync(
                    p => p.Id == item.Product.Id,
                    cancellationToken);

            if (product is null)
                throw new KeyNotFoundException(
                    "Product was not found.");

            if (product.UnitPrice != item.UnitPrice)
                throw new InvalidOperationException(
                    "The product price has changed.");

            if (product.Stock < item.Quantity)
                throw new InvalidOperationException(
                    "Insufficient stock for this product.");

            // El inventario se descuenta solo
            // cuando la transacción pueda guardarse.
            product.Stock -= item.Quantity;

            itemRecords.Add(new SaleItemRecord
            {
                Id = Guid.NewGuid(),
                SaleId = saleId,
                ProductVariantId = product.Id,
                Sku = product.Sku,
                ProductName = product.Name,
                Size = product.Size,
                Color = product.Color,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Subtotal
            });
        }

        var totals = sale.Totals;

        var saleRecord = new SaleRecord
        {
            Id = saleId,
            CashSessionId = cashSessionId,
            CreatedAt = _timeProvider.GetUtcNow(),
            Subtotal = totals.Subtotal,
            Tax = totals.Tax,
            Total = totals.Total,
            Change = summary.Change,
            Items = itemRecords,
            Payments = paymentList.Select(payment =>
                new SalePaymentRecord
                {
                    Id = Guid.NewGuid(),
                    SaleId = saleId,
                    Method = payment.Method,
                    Amount = payment.Amount
                }).ToList()
        };

        _db.Sales.Add(saleRecord);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Solo después de guardar correctamente
        // marcamos la venta en memoria como completada.
        sale.Complete(paymentList);

        return saleId;
    }
}
