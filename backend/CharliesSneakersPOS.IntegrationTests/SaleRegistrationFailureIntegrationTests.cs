
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Infrastructure.Persistence;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharliesSneakersPOS.IntegrationTests;

public class SaleRegistrationFailureIntegrationTests
{
    [Fact]
    public async Task RejectsInsufficientDatabaseStock()
    {
        await WithTemporaryDatabaseAsync(async options =>
        {
            // En SQL Server solamente queda una unidad.
            var (productId, cashSessionId) =
                await SeedDatabaseAsync(options, stock: 1);

            // El carrito tiene información desactualizada:
            // cree que existen tres unidades.
            var sale = CreateSale(
                productId, quantity: 2, knownStock: 3);

            await using (var db = new AppDbContext(options))
            {
                var service = new SaleRegistrationService(db);

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => service.RegisterAsync(
                        Guid.NewGuid(),
                        cashSessionId,
                        sale,
                        CreatePayments(sale)));
            }

            Assert.False(sale.IsCompleted);

            // No se guardó nada ni cambió el stock.
            await AssertPersistenceAsync(
                options, productId, expectedStock: 1,
                expectedSales: 0);
        });
    }

    [Fact]
    public async Task RejectsUnknownCashSession()
    {
        await WithTemporaryDatabaseAsync(async options =>
        {
            var (productId, _) =
                await SeedDatabaseAsync(options, stock: 3);

            var sale = CreateSale(productId);

            await using (var db = new AppDbContext(options))
            {
                var service = new SaleRegistrationService(db);

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => service.RegisterAsync(
                        Guid.NewGuid(),
                        Guid.NewGuid(), // Sesión inexistente
                        sale,
                        CreatePayments(sale)));
            }

            Assert.False(sale.IsCompleted);

            await AssertPersistenceAsync(
                options, productId, expectedStock: 3,
                expectedSales: 0);
        });
    }

    [Fact]
    public async Task RejectsDuplicateSaleWithoutDoubleStockDeduction()
    {
        await WithTemporaryDatabaseAsync(async options =>
        {
            var (productId, cashSessionId) =
                await SeedDatabaseAsync(options, stock: 3);

            var saleId = Guid.NewGuid();

            // Registrar la primera venta.
            await using (var db = new AppDbContext(options))
            {
                var sale = CreateSale(productId);
                var service = new SaleRegistrationService(db);

                await service.RegisterAsync(
                    saleId,
                    cashSessionId,
                    sale,
                    CreatePayments(sale));

                Assert.True(sale.IsCompleted);
            }

            // Intentar registrar otra venta con el mismo ID.
            await using (var db = new AppDbContext(options))
            {
                var duplicateSale = CreateSale(productId);
                var service = new SaleRegistrationService(db);

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => service.RegisterAsync(
                        saleId,
                        cashSessionId,
                        duplicateSale,
                        CreatePayments(duplicateSale)));

                Assert.False(duplicateSale.IsCompleted);
            }

            // Solo existe una venta y el stock bajó de 3 a 2.
            await AssertPersistenceAsync(
                options, productId, expectedStock: 2,
                expectedSales: 1);
        });
    }

    private static Sale CreateSale(
        Guid productId,
        int quantity = 1,
        int knownStock = 3)
    {
        var product = new ProductVariant(
            productId,
            "AJ-TEST",
            "Air Jordan Test",
            "42",
            "Black",
            10000m,
            knownStock);

        var sale = new Sale();
        sale.AddItem(product, quantity);
        return sale;
    }

    private static Payment[] CreatePayments(Sale sale)
    {
        return
        [
            new Payment(
                PaymentMethod.Cash,
                sale.Totals.Total)
        ];
    }

    private static async Task<(Guid ProductId, Guid CashSessionId)>
        SeedDatabaseAsync(
            DbContextOptions<AppDbContext> options,
            int stock)
    {
        await using var db = new AppDbContext(options);

        await db.Database.MigrateAsync();

        var cashSession = CashSession.Open(
            50000m,
            DateTimeOffset.UtcNow);

        var productId = Guid.NewGuid();

        db.CashSessions.Add(cashSession);

        db.ProductVariants.Add(new ProductVariantRecord
        {
            Id = productId,
            Sku = "AJ-TEST",
            Name = "Air Jordan Test",
            Size = "42",
            Color = "Black",
            UnitPrice = 10000m,
            Stock = stock
        });

        await db.SaveChangesAsync();

        return (productId, cashSession.Id);
    }

    private static async Task AssertPersistenceAsync(
        DbContextOptions<AppDbContext> options,
        Guid productId,
        int expectedStock,
        int expectedSales)
    {
        await using var db = new AppDbContext(options);

        var product = await db.ProductVariants
            .SingleAsync(p => p.Id == productId);

        Assert.Equal(expectedStock, product.Stock);
        Assert.Equal(expectedSales, await db.Sales.CountAsync());
        Assert.Equal(expectedSales, await db.SaleItems.CountAsync());
        Assert.Equal(expectedSales, await db.SalePayments.CountAsync());
    }

    private static async Task WithTemporaryDatabaseAsync(
        Func<DbContextOptions<AppDbContext>, Task> action)
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
            await action(options);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
