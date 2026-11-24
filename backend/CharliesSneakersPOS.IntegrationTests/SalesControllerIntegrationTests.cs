
using CharliesSneakersPOS.Api.Contracts;
using CharliesSneakersPOS.Api.Controllers;
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Infrastructure.Persistence;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharliesSneakersPOS.IntegrationTests;

public class SalesControllerIntegrationTests
{
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task RegisterValidSalePersistsPaymentsAndStock()
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
            var saleId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            await using (var db = new AppDbContext(options))
            {
                await db.Database.MigrateAsync();

                var cashSession = CashSession.Open(
                    50000m,
                    DateTimeOffset.UtcNow);

                db.CashSessions.Add(cashSession);

                db.ProductVariants.Add(new ProductVariantRecord
                {
                    Id = productId,
                    Sku = "AJ-API",
                    Name = "Air Jordan API",
                    Size = "42",
                    Color = "Black",
                    UnitPrice = 10000m,
                    Stock = 3
                });

                await db.SaveChangesAsync();

                var request = new RegisterSaleRequest(
                    saleId,
                    cashSession.Id,
                    [new SaleProductRequest(productId, 1)],
                    [new SalePaymentRequest("Cash", 15000m)]);

                var controller = new SalesController(
                    db,
                    new SaleRegistrationService(db));

                var result = await controller.Register(
                    request,
                    CancellationToken.None);

                var ok = Assert.IsType<OkObjectResult>(
                    result.Result);

                var response =
                    Assert.IsType<RegisterSaleResponse>(ok.Value);

                Assert.Equal(saleId, response.SaleId);
                Assert.Equal(10000m, response.Subtotal);
                Assert.Equal(1300m, response.Tax);
                Assert.Equal(11300m, response.Total);
                Assert.Equal(3700m, response.Change);
            }

            // Verificar los datos desde otro contexto.
            await using (var verification = new AppDbContext(options))
            {
                var sale = await verification.Sales
                    .Include(s => s.Items)
                    .Include(s => s.Payments)
                    .SingleAsync(s => s.Id == saleId);

                var product = await verification.ProductVariants
                    .SingleAsync(p => p.Id == productId);

                Assert.Single(sale.Items);
                Assert.Single(sale.Payments);
                Assert.Equal(11300m, sale.Total);
                Assert.Equal(2, product.Stock);
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
