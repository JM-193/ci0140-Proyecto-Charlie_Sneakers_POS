using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CharliesSneakersPOS.Infrastructure.Persistence;

public sealed class EfCashSessionRepository : ICashSessionRepository, ICashSessionStatus
{
    private readonly AppDbContext _db;

    public EfCashSessionRepository(AppDbContext db) => _db = db;

    public Task<CashSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.CashSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<CashSession?> GetOpenAsync(CancellationToken cancellationToken = default)
        => _db.CashSessions.FirstOrDefaultAsync(
            s => s.Status == CashSessionStatus.Open, cancellationToken);

    public async Task AddAsync(CashSession session, CancellationToken cancellationToken = default)
        => await _db.CashSessions.AddAsync(session, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public Task<bool> IsOpenAsync(Guid cashSessionId, CancellationToken cancellationToken = default)
        => _db.CashSessions.AnyAsync(
            s => s.Id == cashSessionId && s.Status == CashSessionStatus.Open, cancellationToken);
}
