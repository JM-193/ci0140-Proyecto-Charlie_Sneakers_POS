
using CharliesSneakersPOS.Api.Contracts;
using CharliesSneakersPOS.Api.Controllers;
using CharliesSneakersPOS.Infrastructure.Persistence;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharliesSneakersPOS.IntegrationTests;

public class ProductsControllerIntegrationTests
{
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task SearchProducts_FiltersBySkuBarcodeAndSize()
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
            await using var db = new AppDbContext(options);
            await db.Database.MigrateAsync();

            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();

            db.ProductVariants.AddRange(
                new ProductVariantRecord
                {
                    Id = firstId,
                    Sku = "AJ-042",
                    Barcode = "123456789",
                    Name = "Air Jordan",
                    Size = "42",
                    Color = "Black",
                    UnitPrice = 10000m,
                    Stock = 3
                },
                new ProductVariantRecord
                {
                    Id = secondId,
                    Sku = "AJ-043",
                    Barcode = "987654321",
                    Name = "Air Jordan",
                    Size = "43",
                    Color = "Red",
                    UnitPrice = 12000m,
                    Stock = 2
                });

            await db.SaveChangesAsync();

            var controller = new ProductsController(db);

            // Buscar por SKU.
            var bySku = ReadResults(
                await controller.Get(
                    "AJ-042", null, null, CancellationToken.None));

            Assert.Single(bySku);
            Assert.Equal(firstId, bySku[0].Id);

            // Buscar por código de barras.
            var byBarcode = ReadResults(
                await controller.Get(
                    "987654321", null, null, CancellationToken.None));

            Assert.Single(byBarcode);
            Assert.Equal(secondId, byBarcode[0].Id);

            // Buscar por nombre, talla y color.
            var filtered = ReadResults(
                await controller.Get(
                    "Jordan", "42", "Black", CancellationToken.None));

            Assert.Single(filtered);
            Assert.Equal(firstId, filtered[0].Id);
            Assert.Equal(10000m, filtered[0].UnitPrice);
            Assert.Equal(3, filtered[0].Stock);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static IReadOnlyList<ProductVariantResponse> ReadResults(
        ActionResult<IReadOnlyList<ProductVariantResponse>> response)
    {
        var ok = Assert.IsType<OkObjectResult>(response.Result);

        return Assert.IsAssignableFrom<
            IReadOnlyList<ProductVariantResponse>>(ok.Value);
    }
}
