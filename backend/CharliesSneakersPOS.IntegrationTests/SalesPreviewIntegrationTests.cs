using System.Net;
using System.Net.Http.Json;
using CharliesSneakersPOS.Api.Contracts;
using CharliesSneakersPOS.Infrastructure.Persistence;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CharliesSneakersPOS.IntegrationTests;

// POST /api/sales/preview (HU-03 totales en vivo, HU-05 pendiente y vuelto)
// contra SQL Server real con una base temporal.
public class SalesPreviewIntegrationTests
{
    private const string PreviewUrl = "/api/sales/preview";

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task PreviewReturnsTotalsWithoutPayments()
    {
        await using var db = await TestDatabase.CreateAsync("PreviewTotals");
        var productId = await db.SeedProductAsync("PV-TOTALS", stock: 5);
        using var client = db.CreateClient();

        // 2 x 10000 = 20000; IVA 13 % = 2600; total 22600.
        var response = await client.PostAsJsonAsync(
            PreviewUrl, NewRequest(productId, 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PreviewSaleResponse>();
        Assert.NotNull(body);
        Assert.Equal(20000m, body.Subtotal);
        Assert.Equal(2600m, body.Tax);
        Assert.Equal(22600m, body.Total);
        Assert.Equal(0m, body.TotalReceived);
        Assert.Equal(22600m, body.Remaining);
        Assert.Equal(0m, body.Change);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task PreviewCalculatesRemainingAndChangeFromPayments()
    {
        await using var db = await TestDatabase.CreateAsync("PreviewPayments");
        var productId = await db.SeedProductAsync("PV-PAY", stock: 5);
        using var client = db.CreateClient();

        // Total 11300. Pago parcial con tarjeta: queda pendiente.
        var partial = await PreviewAsync(client, productId, ("Card", 5000m));
        Assert.Equal(5000m, partial.TotalReceived);
        Assert.Equal(6300m, partial.Remaining);
        Assert.Equal(0m, partial.Change);

        // Efectivo mayor al total: vuelto.
        var cash = await PreviewAsync(client, productId, ("Cash", 20000m));
        Assert.Equal(0m, cash.Remaining);
        Assert.Equal(8700m, cash.Change);

        // Mixto: SINPE + efectivo; el vuelto sale solo del efectivo.
        var mixed = await PreviewAsync(
            client, productId, ("SinpeMobile", 6300m), ("Cash", 10000m));
        Assert.Equal(16300m, mixed.TotalReceived);
        Assert.Equal(0m, mixed.Remaining);
        Assert.Equal(5000m, mixed.Change);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task PreviewDoesNotPersistSalesNorChangeStock()
    {
        await using var db = await TestDatabase.CreateAsync("PreviewNoPersist");
        var productId = await db.SeedProductAsync("PV-NOSAVE", stock: 3);
        using var client = db.CreateClient();

        await PreviewAsync(client, productId, ("Cash", 50000m));

        await using var verification = db.CreateContext();
        Assert.Equal(0, await verification.Sales.CountAsync());
        var product = await verification.ProductVariants.SingleAsync(p => p.Id == productId);
        Assert.Equal(3, product.Stock);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task PreviewRejectsInvalidSalesWithStableErrorCodes()
    {
        await using var db = await TestDatabase.CreateAsync("PreviewErrors");
        var productId = await db.SeedProductAsync("PV-ERR", stock: 2);
        var soldOutId = await db.SeedProductAsync("PV-SOLDOUT", stock: 0);
        using var client = db.CreateClient();

        await AssertErrorAsync(client, NewRequest(productId, 3),
            HttpStatusCode.Conflict, PreviewSaleErrorCodes.InsufficientStock);

        await AssertErrorAsync(client, NewRequest(soldOutId, 1),
            HttpStatusCode.Conflict, PreviewSaleErrorCodes.InsufficientStock);

        await AssertErrorAsync(client, NewRequest(productId, 0),
            HttpStatusCode.BadRequest, PreviewSaleErrorCodes.InvalidQuantity);

        await AssertErrorAsync(client, NewRequest(Guid.NewGuid(), 1),
            HttpStatusCode.NotFound, PreviewSaleErrorCodes.ProductNotFound);

        await AssertErrorAsync(client, new PreviewSaleRequest([], null),
            HttpStatusCode.BadRequest, PreviewSaleErrorCodes.InvalidRequest);

        await AssertErrorAsync(client, NewRequest(productId, 1, ("Bitcoin", 100m)),
            HttpStatusCode.BadRequest, PreviewSaleErrorCodes.InvalidPayment);

        await AssertErrorAsync(client, NewRequest(productId, 1, ("Cash", 0m)),
            HttpStatusCode.BadRequest, PreviewSaleErrorCodes.InvalidPayment);

        // Solo el efectivo puede superar el pendiente (total 11300).
        await AssertErrorAsync(client, NewRequest(productId, 1, ("Card", 20000m)),
            HttpStatusCode.Conflict, PreviewSaleErrorCodes.PaymentNotAllowed);
    }

    private static PreviewSaleRequest NewRequest(
        Guid productId, int quantity, params (string Method, decimal Amount)[] payments)
        => new(
            [new SaleProductRequest(productId, quantity)],
            payments.Select(p => new SalePaymentRequest(p.Method, p.Amount)).ToList());

    private static async Task<PreviewSaleResponse> PreviewAsync(
        HttpClient client, Guid productId, params (string Method, decimal Amount)[] payments)
    {
        var response = await client.PostAsJsonAsync(
            PreviewUrl, NewRequest(productId, 1, payments));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PreviewSaleResponse>())!;
    }

    private static async Task AssertErrorAsync(
        HttpClient client,
        PreviewSaleRequest request,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        var response = await client.PostAsJsonAsync(PreviewUrl, request);

        Assert.Equal(expectedStatus, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<PreviewSaleError>();
        Assert.NotNull(error);
        Assert.Equal(expectedCode, error.Code);
    }

    // Base temporal con migraciones aplicadas; se elimina al terminar.
    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly DbContextOptions<AppDbContext> _options;
        private WebApplicationFactory<Program>? _factory;

        private TestDatabase(string connectionString)
        {
            _connectionString = connectionString;
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;
        }

        public static async Task<TestDatabase> CreateAsync(string prefix)
        {
            var name = $"CharliesSneakersPOS_{prefix}_{Guid.NewGuid():N}";
            var database = new TestDatabase(TestSqlServer.ConnectionString(name));

            await using var context = database.CreateContext();
            await context.Database.MigrateAsync();
            return database;
        }

        public AppDbContext CreateContext() => new(_options);

        public async Task<Guid> SeedProductAsync(string sku, int stock)
        {
            var id = Guid.NewGuid();
            await using var context = CreateContext();
            context.ProductVariants.Add(new ProductVariantRecord
            {
                Id = id,
                Sku = sku,
                Name = "Air Jordan Preview",
                Size = "42",
                Color = "Black",
                UnitPrice = 10000m,
                Stock = stock
            });
            await context.SaveChangesAsync();
            return id;
        }

        public HttpClient CreateClient()
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = _connectionString
                    })));

            return _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        }

        public async ValueTask DisposeAsync()
        {
            _factory?.Dispose();
            await using var cleanup = CreateContext();
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
