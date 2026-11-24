using CharliesSneakersPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CharliesSneakersPOS.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<CashSession> CashSessions => Set<CashSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
