
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Infrastructure.Persistence;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharliesSneakersPOS.IntegrationTests;

public class SaleRegistrationIntegrationTests
{
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task RegisterAsync_ValidSale_PersistsAndDeductsStock()
    {
        // Se crea una base nueva para esta prueba.
        var databaseName =
            $"CharliesSneakersPOS_Test_{Guid.NewGuid():N}";

        var connectionString =
            TestSqlServer.ConnectionString(databaseName);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        try
        {
            var productId = Guid.NewGuid();
            Guid saleId;

            await using (var db = new AppDbContext(options))
            {
                // Crear el esquema usando las migraciones reales.
                await db.Database.MigrateAsync();

                // Registrar una sesión de caja abierta.
                var cashSession = CashSession.Open(
                    50000m,
                    DateTimeOffset.UtcNow);

                db.CashSessions.Add(cashSession);

                // Registrar un producto con 3 unidades.
                db.ProductVariants.Add(new ProductVariantRecord
                {
                    Id = productId,
                    Sku = "AJ-001",
                    Name = "Air Jordan 1",
                    Size = "42",
                    Color = "Black",
                    UnitPrice = 10000m,
                    Stock = 3
                });

                await db.SaveChangesAsync();

                // Crear la venta utilizando el dominio.
                var product = new ProductVariant(
                    productId,
                    "AJ-001",
                    "Air Jordan 1",
                    "42",
                    "Black",
                    10000m,
                    3);

                var sale = new Sale();
                sale.AddItem(product, 1);

                var payments = new[]
                {
                    new Payment(PaymentMethod.Cash, 11300m)
                };

                var service = new SaleRegistrationService(db);

                saleId = await service.RegisterAsync(
                    Guid.NewGuid(),
                    cashSession.Id,
                    sale,
                    payments);

                Assert.True(sale.IsCompleted);
            }

            // Consultar desde otro DbContext para comprobar
            // que los datos realmente se guardaron.
            await using (var verification = new AppDbContext(options))
            {
                var savedSale = await verification.Sales
                    .Include(s => s.Items)
                    .Include(s => s.Payments)
                    .SingleAsync(s => s.Id == saleId);

                var savedProduct = await verification.ProductVariants
                    .SingleAsync(p => p.Id == productId);

                Assert.Equal(10000m, savedSale.Subtotal);
                Assert.Equal(1300m, savedSale.Tax);
                Assert.Equal(11300m, savedSale.Total);

                Assert.Single(savedSale.Items);
                Assert.Single(savedSale.Payments);

                // Inventario original: 3.
                // Inventario esperado después de la venta: 2.
                Assert.Equal(2, savedProduct.Stock);
            }
        }
        finally
        {
            // Eliminar únicamente la base temporal
            // creada para esta prueba.
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
