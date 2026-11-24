
using CharliesSneakersPOS.Domain.Interfaces;
using CharliesSneakersPOS.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace CharliesSneakersPOS.Infrastructure.Persistence;

public sealed class EfCashSalesProvider : ICashSalesProvider
{
    private readonly AppDbContext _db;

    public EfCashSalesProvider(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<decimal> GetNetCashSalesAsync(
        Guid cashSessionId,
        CancellationToken cancellationToken = default)
    {
        if (cashSessionId == Guid.Empty)
            throw new ArgumentException(
                "A valid cash session ID is required.",
                nameof(cashSessionId));

        // Consultar únicamente las ventas de esta sesión.
        var sales = await _db.Sales
            .AsNoTracking()
            .Where(s => s.CashSessionId == cashSessionId)
            .Select(s => new
            {
                s.Change,
                CashReceived = s.Payments
                    .Where(p => p.Method == PaymentMethod.Cash)
                    .Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken);

        // Efectivo neto = efectivo recibido - vuelto.
        return sales.Sum(s => s.CashReceived - s.Change);
    }
}
