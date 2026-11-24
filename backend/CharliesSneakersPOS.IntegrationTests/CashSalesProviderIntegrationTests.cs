
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Infrastructure.Persistence;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharliesSneakersPOS.IntegrationTests;

public class CashSalesProviderIntegrationTests
{
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task NetCashSalesCalculatesMixedPaymentsCorrectly()
    {
        var databaseName =
            $"CharliesSneakersPOS_Test_{Guid.NewGuid():N}";

        var connectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        try
        {
            await using var db = new AppDbContext(options);

            await db.Database.MigrateAsync();

            var session = CashSession.Open(
                50000m,
                DateTimeOffset.UtcNow);

            var productId = Guid.NewGuid();

            db.CashSessions.Add(session);

            db.ProductVariants.Add(new ProductVariantRecord
            {
                Id = productId,
                Sku = "AJ-CASH",
                Name = "Air Jordan Test",
                Size = "42",
                Color = "Black",
                UnitPrice = 10000m,
                Stock = 3
            });

            await db.SaveChangesAsync();

            var registration = new SaleRegistrationService(db);

            // Venta 1: pago exacto en efectivo.
            await registration.RegisterAsync(
                Guid.NewGuid(),
                session.Id,
                CreateSale(productId),
                new[]
                {
                    new Payment(PaymentMethod.Cash, 11300m)
                });

            // Venta 2: pago combinado.
            // Efectivo neto: 10000 - 3700 = 6300.
            await registration.RegisterAsync(
                Guid.NewGuid(),
                session.Id,
                CreateSale(productId),
                new[]
                {
                    new Payment(PaymentMethod.Card, 5000m),
                    new Payment(PaymentMethod.Cash, 10000m)
                });

            // Venta 3: pago solamente con tarjeta.
            await registration.RegisterAsync(
                Guid.NewGuid(),
                session.Id,
                CreateSale(productId),
                new[]
                {
                    new Payment(PaymentMethod.Card, 11300m)
                });

            var provider = new EfCashSalesProvider(db);

            var netCash = await provider.GetNetCashSalesAsync(
                session.Id);

            // 11300 + 6300 + 0 = 17600.
            Assert.Equal(17600m, netCash);

            // Una sesión sin ventas devuelve cero.
            var unknownSessionCash =
                await provider.GetNetCashSalesAsync(Guid.NewGuid());

            Assert.Equal(0m, unknownSessionCash);

            // Las tres ventas quedaron registradas.
            Assert.Equal(3, await db.Sales.CountAsync());

            // El inventario pasó de 3 a 0.
            var product = await db.ProductVariants
                .SingleAsync(p => p.Id == productId);

            Assert.Equal(0, product.Stock);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static Sale CreateSale(Guid productId)
    {
        var product = new ProductVariant(
            productId,
            "AJ-CASH",
            "Air Jordan Test",
            "42",
            "Black",
            10000m,
            3
        );

        var sale = new Sale();
        sale.AddItem(product, 1);

        return sale;
    }
}
