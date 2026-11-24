
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Interfaces;
using CharliesSneakersPOS.Domain.Payments;

namespace CharliesSneakersPOS.Domain.Services;

public sealed class SaleCompletionService
{
    private readonly ICashSessionStatus _cashSessionStatus;

    public SaleCompletionService(
        ICashSessionStatus cashSessionStatus)
    {
        _cashSessionStatus = cashSessionStatus
            ?? throw new ArgumentNullException(
                nameof(cashSessionStatus));
    }

    public async Task CompleteAsync(
        Sale sale,
        Guid cashSessionId,
        IEnumerable<Payment> payments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(payments);

        if (cashSessionId == Guid.Empty)
            throw new ArgumentException(
                "A valid cash session is required.",
                nameof(cashSessionId));

        if (sale.IsCompleted)
            throw new InvalidOperationException(
                "The sale has already been completed.");

        bool isOpen = await _cashSessionStatus.IsOpenAsync(
            cashSessionId,
            cancellationToken);

        if (!isOpen)
            throw new InvalidOperationException(
                "The cash register must be open to complete a sale.");

        sale.Complete(payments);
    }
}
