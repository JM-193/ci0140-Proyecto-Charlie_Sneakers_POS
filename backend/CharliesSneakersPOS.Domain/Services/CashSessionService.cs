using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Interfaces;

namespace CharliesSneakersPOS.Domain.Services;

public sealed class CashSessionService
{
    private readonly ICashSessionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CashSessionService(ICashSessionRepository repository, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<CashSession> OpenAsync(
        decimal openingAmount,
        CancellationToken cancellationToken = default)
    {
        if (await _repository.GetOpenAsync(cancellationToken) is not null)
            throw new InvalidOperationException("There is already an open cash session.");

        var session = CashSession.Open(openingAmount, _timeProvider.GetUtcNow());

        await _repository.AddAsync(session, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return session;
    }
}
