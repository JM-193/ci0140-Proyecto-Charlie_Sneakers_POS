
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Infrastructure.Persistence;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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


    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task ConcurrentSales_CannotOversellLastUnit()
    {
        await WithTemporaryDatabaseAsync(async options =>
        {
            // Solo queda una unidad en SQL Server.
            var (productId, cashSessionId) =
                await SeedDatabaseAsync(options, stock: 1);

            var start = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            async Task<bool> AttemptSaleAsync()
            {
                await start.Task;

                await using var db = new AppDbContext(options);

                var sale = CreateSale(
                    productId,
                    quantity: 1,
                    knownStock: 1);

                var service = new SaleRegistrationService(db);

                try
                {
                    await service.RegisterAsync(
                        Guid.NewGuid(),
                        cashSessionId,
                        sale,
                        CreatePayments(sale));

                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
                catch (DbUpdateConcurrencyException)
                {
                    return false;
                }
                catch (DbUpdateException ex)
                    when (ex.InnerException is SqlException
                        { Number: 1205 or 3960 })
                {
                    return false;
                }
                catch (SqlException ex)
                    when (ex.Number is 1205 or 3960)
                {
                    return false;
                }
            }

            // Las dos operaciones comienzan juntas.
            var firstSale = AttemptSaleAsync();
            var secondSale = AttemptSaleAsync();

            start.SetResult(true);

            var results = await Task.WhenAll(
                firstSale,
                secondSale);

            // Solo una venta puede completarse.
            Assert.Equal(1, results.Count(success => success));

            await using var verification =
                new AppDbContext(options);

            var product = await verification.ProductVariants
                .SingleAsync(p => p.Id == productId);

            Assert.Equal(0, product.Stock);

            Assert.Equal(
                1,
                await verification.Sales.CountAsync());

            Assert.Equal(
                1,
                await verification.SalePayments.CountAsync());
        });
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
            TestSqlServer.ConnectionString(databaseName);

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

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task RejectsClosedCashSession()
    {
        await WithTemporaryDatabaseAsync(async options =>
        {
            var (productId, cashSessionId) =
                await SeedDatabaseAsync(options, stock: 3);

            // Simular una caja cerrada en SQL Server.
            await using (var setup = new AppDbContext(options))
            {
                var affected = await setup.CashSessions
                    .Where(s => s.Id == cashSessionId)
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            s => s.Status,
                            CashSessionStatus.Closed));

                Assert.Equal(1, affected);
            }

            var sale = CreateSale(productId);

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

            // La venta no debe guardarse ni descontar stock.
            await AssertPersistenceAsync(
                options,
                productId,
                expectedStock: 3,
                expectedSales: 0);
        });
    }


    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task RejectsSaleWhenProductPriceChanged()
    {
        await WithTemporaryDatabaseAsync(async options =>
        {
            var (productId, cashSessionId) =
                await SeedDatabaseAsync(options, stock: 3);

            // El carrito conserva el precio anterior: 10000.
            var sale = CreateSale(productId);

            // El precio actual en SQL Server cambia a 11000.
            await using (var setup = new AppDbContext(options))
            {
                var affected = await setup.ProductVariants
                    .Where(p => p.Id == productId)
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            p => p.UnitPrice,
                            11000m));

                Assert.Equal(1, affected);
            }

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

            await AssertPersistenceAsync(
                options,
                productId,
                expectedStock: 3,
                expectedSales: 0);
        });
    }
}
