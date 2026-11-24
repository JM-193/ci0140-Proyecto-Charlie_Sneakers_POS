
using System.Net;
using System.Net.Http.Json;
using CharliesSneakersPOS.Api.Contracts;
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Infrastructure.Persistence;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CharliesSneakersPOS.IntegrationTests;

public class SalesHttpIntegrationTests
{
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task ProductsAndSalesWorkOverHttp()
    {
        var databaseName =
            $"CharliesSneakersPOS_HttpTest_{Guid.NewGuid():N}";

        var connectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        try
        {
            var productId = Guid.NewGuid();
            var saleId = Guid.NewGuid();
            Guid cashSessionId;

            // Preparar una base temporal.
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.MigrateAsync();

                var cashSession = CashSession.Open(
                    50000m,
                    DateTimeOffset.UtcNow);

                cashSessionId = cashSession.Id;

                setup.CashSessions.Add(cashSession);

                setup.ProductVariants.Add(
                    new ProductVariantRecord
                    {
                        Id = productId,
                        Sku = "AJ-HTTP",
                        Name = "Air Jordan HTTP",
                        Size = "42",
                        Color = "Black",
                        UnitPrice = 10000m,
                        Stock = 3
                    });

                await setup.SaveChangesAsync();
            }

            // Iniciar la API con la base temporal.
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                        {
                            configuration.AddInMemoryCollection(
                                new Dictionary<string, string?>
                                {
                                    ["ConnectionStrings:Default"] =
                                        connectionString
                                });
                        });
                });

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });

            // Protección: verificar que el servidor de pruebas
            // apunta a nuestra base temporal, no a la principal.
            using (var scope = factory.Services.CreateScope())
            {
                var configuredDb = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                Assert.Equal(
                    databaseName,
                    configuredDb.Database.GetDbConnection().Database);
            }

            // 1. GET: buscar el producto.
            var searchResponse =
                await client.GetAsync("/api/products?q=AJ-HTTP");

            Assert.Equal(
                HttpStatusCode.OK,
                searchResponse.StatusCode);

            var products = await searchResponse.Content
                .ReadFromJsonAsync<List<ProductVariantResponse>>();

            Assert.NotNull(products);
            var found = Assert.Single(products);
            Assert.Equal(productId, found.Id);

            // 2. POST: registrar una venta.
            var request = new RegisterSaleRequest(
                saleId,
                cashSessionId,
                [new SaleProductRequest(productId, 1)],
                [new SalePaymentRequest("Cash", 15000m)]);

            var saleResponse =
                await client.PostAsJsonAsync("/api/sales", request);

            Assert.Equal(
                HttpStatusCode.OK,
                saleResponse.StatusCode);

            var result = await saleResponse.Content
                .ReadFromJsonAsync<RegisterSaleResponse>();

            Assert.NotNull(result);
            Assert.Equal(saleId, result.SaleId);
            Assert.Equal(11300m, result.Total);
            Assert.Equal(3700m, result.Change);

            // 3. POST: rechazar una venta duplicada.
            var duplicate =
                await client.PostAsJsonAsync("/api/sales", request);

            Assert.Equal(
                HttpStatusCode.Conflict,
                duplicate.StatusCode);

            // 4. Comprobar la persistencia real.
            await using var verification = new AppDbContext(options);

            Assert.Equal(
                1,
                await verification.Sales.CountAsync());

            var product = await verification.ProductVariants
                .SingleAsync(p => p.Id == productId);

            Assert.Equal(2, product.Stock);

            // 5. Producto inexistente: HTTP 404.
            var missingProductRequest = new RegisterSaleRequest(
                Guid.NewGuid(),
                cashSessionId,
                [new SaleProductRequest(Guid.NewGuid(), 1)],
                [new SalePaymentRequest("Cash", 11300m)]);

            var missingProductResponse =
                await client.PostAsJsonAsync(
                    "/api/sales",
                    missingProductRequest);

            Assert.Equal(
                HttpStatusCode.NotFound,
                missingProductResponse.StatusCode);

            // 6. Método de pago inválido: HTTP 400.
            var invalidPaymentRequest = new RegisterSaleRequest(
                Guid.NewGuid(),
                cashSessionId,
                [new SaleProductRequest(productId, 1)],
                [new SalePaymentRequest("InvalidMethod", 11300m)]);

            var invalidPaymentResponse =
                await client.PostAsJsonAsync(
                    "/api/sales",
                    invalidPaymentRequest);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                invalidPaymentResponse.StatusCode);

            // 7. Pago insuficiente: HTTP 409.
            var incompletePaymentRequest = new RegisterSaleRequest(
                Guid.NewGuid(),
                cashSessionId,
                [new SaleProductRequest(productId, 1)],
                [new SalePaymentRequest("Cash", 5000m)]);

            var incompletePaymentResponse =
                await client.PostAsJsonAsync(
                    "/api/sales",
                    incompletePaymentRequest);

            Assert.Equal(
                HttpStatusCode.Conflict,
                incompletePaymentResponse.StatusCode);
        }
        finally
        {
            // Limpiar únicamente la base temporal.
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
