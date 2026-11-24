using CharliesSneakersPOS.Domain.Entities;

namespace CharliesSneakersPOS.Domain.Interfaces;

public interface ICashSessionRepository
{
    Task<CashSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Para la demostración solo existe una caja abierta a la vez.
    Task<CashSession?> GetOpenAsync(CancellationToken cancellationToken = default);

    Task AddAsync(CashSession session, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
