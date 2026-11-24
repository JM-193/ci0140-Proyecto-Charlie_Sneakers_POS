using CharliesSneakersPOS.Domain.Interfaces;
using CharliesSneakersPOS.Domain.Models;

namespace CharliesSneakersPOS.Domain.Services;

public sealed class CashClosingService
{
    private readonly ICashSessionRepository _repository;
    private readonly ICashSalesProvider _salesProvider;
    private readonly TimeProvider _timeProvider;

    public CashClosingService(
        ICashSessionRepository repository,
        ICashSalesProvider salesProvider,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(salesProvider);
        _repository = repository;
        _salesProvider = salesProvider;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<CashClosingSummary> CloseAsync(
        Guid cashSessionId,
        decimal countedAmount,
        CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetByIdAsync(cashSessionId, cancellationToken)
            ?? throw new KeyNotFoundException("Cash session not found.");

        // Se valida antes de consultar ventas para rechazar cierres duplicados rápido.
        if (!session.IsOpen)
            throw new InvalidOperationException("The cash session is already closed.");

        var netCashSales = await _salesProvider.GetNetCashSalesAsync(
            cashSessionId, cancellationToken);

        var summary = session.Close(countedAmount, netCashSales, _timeProvider.GetUtcNow());

        await _repository.SaveChangesAsync(cancellationToken);

        return summary;
    }
}
