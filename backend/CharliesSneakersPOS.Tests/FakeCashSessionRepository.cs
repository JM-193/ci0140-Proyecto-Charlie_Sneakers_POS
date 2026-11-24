using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Interfaces;

namespace CharliesSneakersPOS.Tests;

// Fake en memoria compartido por las pruebas de caja.
internal sealed class FakeCashSessionRepository : ICashSessionRepository, ICashSessionStatus
{
    public List<CashSession> Sessions { get; } = new();
    public int SaveCount { get; private set; }

    public Task<CashSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Sessions.FirstOrDefault(s => s.Id == id));

    public Task<CashSession?> GetOpenAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Sessions.FirstOrDefault(s => s.IsOpen));

    public Task AddAsync(CashSession session, CancellationToken cancellationToken = default)
    {
        Sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<bool> IsOpenAsync(Guid cashSessionId, CancellationToken cancellationToken = default)
        => Task.FromResult(Sessions.Any(s => s.Id == cashSessionId && s.IsOpen));
}
