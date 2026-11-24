
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CharliesSneakersPOS.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<CashSession> CashSessions
        => Set<CashSession>();

    public DbSet<ProductVariantRecord> ProductVariants
        => Set<ProductVariantRecord>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }

    public DbSet<SaleRecord> Sales => Set<SaleRecord>();

    public DbSet<SaleItemRecord> SaleItems => Set<SaleItemRecord>();

    public DbSet<SalePaymentRecord> SalePayments => Set<SalePaymentRecord>();
}
