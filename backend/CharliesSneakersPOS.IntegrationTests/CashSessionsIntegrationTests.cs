using System.Net;
using System.Net.Http.Json;
using CharliesSneakersPOS.Api.Contracts;
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Infrastructure.Persistence;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CharliesSneakersPOS.IntegrationTests;

// Pruebas de la Caja (HU-09, HU-10) contra SQL Server real con bases temporales.
// Servidor: LocalDB por defecto; se puede cambiar con la variable de entorno
// TEST_SQL_SERVER (por ejemplo "localhost\SQLEXPRESS").
public class CashSessionsIntegrationTests
{
    private const string BaseUrl = "/api/cash-sessions";

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task OpenSellInCashAndCloseWorksOverHttp()
    {
        await using var db = await TestDatabase.CreateAsync("CashFlow");
        var productId = await db.SeedProductAsync(stock: 5);
        using var client = db.CreateClient();

        // HU-09: apertura.
        var open = await client.PostAsJsonAsync(BaseUrl, new OpenCashSessionRequest(50000m));
        Assert.Equal(HttpStatusCode.Created, open.StatusCode);
        var session = await open.Content.ReadFromJsonAsync<CashSessionResponse>();
        Assert.NotNull(session);
        Assert.Equal("Open", session.Status);

        var current = await client.GetAsync($"{BaseUrl}/current");
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);

        // Venta en efectivo: total 11300, recibe 15000, vuelto 3700 -> neto 11300.
        await RegisterSaleAsync(client, session.Id, productId, "Cash", 15000m);
        // Venta con tarjeta: no entra al efectivo esperado.
        await RegisterSaleAsync(client, session.Id, productId, "Card", 11300m);

        // HU-10: cierre con faltante de 300 (esperado 61300, contado 61000).
        var close = await client.PostAsJsonAsync(
            $"{BaseUrl}/{session.Id}/close", new CloseCashSessionRequest(61000m));
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        var summary = await close.Content.ReadFromJsonAsync<CashClosingResponse>();
        Assert.NotNull(summary);
        Assert.Equal(61300m, summary.Expected);
        Assert.Equal(61000m, summary.Counted);
        Assert.Equal(300m, summary.Shortage);
        Assert.Equal(0m, summary.Surplus);
        Assert.False(summary.IsBalanced);

        // Quedó persistido como cerrada y ya no hay caja abierta.
        var detail = await client.GetFromJsonAsync<CashSessionResponse>($"{BaseUrl}/{session.Id}");
        Assert.Equal("Closed", detail!.Status);
        Assert.Equal(61300m, detail.ExpectedAmount);
        Assert.Equal(61000m, detail.CountedAmount);
        Assert.NotNull(detail.ClosedAt);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{BaseUrl}/current")).StatusCode);

        // C-03: cierre duplicado rechazado y el primer resultado se conserva.
        var duplicate = await client.PostAsJsonAsync(
            $"{BaseUrl}/{session.Id}/close", new CloseCashSessionRequest(1m));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var after = await client.GetFromJsonAsync<CashSessionResponse>($"{BaseUrl}/{session.Id}");
        Assert.Equal(61000m, after!.CountedAmount);

        // C-03: con la caja cerrada no se puede vender.
        var blockedSale = await client.PostAsJsonAsync(
            "/api/sales", NewSaleRequest(session.Id, productId, "Cash", 15000m));
        Assert.Equal(HttpStatusCode.Conflict, blockedSale.StatusCode);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task CloseWithExactCountIsBalancedAndSurplusIsReported()
    {
        await using var db = await TestDatabase.CreateAsync("CashBalance");
        using var client = db.CreateClient();

        var balanced = await OpenAsync(client, 20000m);
        var closeBalanced = await client.PostAsJsonAsync(
            $"{BaseUrl}/{balanced.Id}/close", new CloseCashSessionRequest(20000m));
        var balancedBody = await closeBalanced.Content.ReadFromJsonAsync<CashClosingResponse>();
        Assert.True(balancedBody!.IsBalanced);

        var surplus = await OpenAsync(client, 20000m);
        var closeSurplus = await client.PostAsJsonAsync(
            $"{BaseUrl}/{surplus.Id}/close", new CloseCashSessionRequest(20500m));
        var surplusBody = await closeSurplus.Content.ReadFromJsonAsync<CashClosingResponse>();
        Assert.Equal(500m, surplusBody!.Surplus);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task OnlyOneCashSessionCanBeOpenAtATime()
    {
        await using var db = await TestDatabase.CreateAsync("CashSingle");
        using var client = db.CreateClient();

        await OpenAsync(client, 10000m);

        var second = await client.PostAsJsonAsync(BaseUrl, new OpenCashSessionRequest(5000m));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        await using var verification = db.CreateContext();
        Assert.Equal(1, await verification.CashSessions.CountAsync());
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task InvalidRequestsReturnExpectedStatusCodes()
    {
        await using var db = await TestDatabase.CreateAsync("CashErrors");
        using var client = db.CreateClient();

        var invalidAmount = await client.PostAsJsonAsync(BaseUrl, new OpenCashSessionRequest(0m));
        Assert.Equal(HttpStatusCode.BadRequest, invalidAmount.StatusCode);

        var unknown = await client.PostAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}/close", new CloseCashSessionRequest(100m));
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var session = await OpenAsync(client, 10000m);
        var negative = await client.PostAsJsonAsync(
            $"{BaseUrl}/{session.Id}/close", new CloseCashSessionRequest(-1m));
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task DatabaseRejectsASecondOpenSession()
    {
        await using var db = await TestDatabase.CreateAsync("CashIndex");
        await using var context = db.CreateContext();

        context.CashSessions.Add(CashSession.Open(1000m, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();

        // Se omite la validación del servicio: debe fallar el índice único filtrado.
        await using var other = db.CreateContext();
        other.CashSessions.Add(CashSession.Open(2000m, DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task ConcurrentClosesAreDetectedByRowVersion()
    {
        await using var db = await TestDatabase.CreateAsync("CashConcurrency");
        await using var setup = db.CreateContext();
        var session = CashSession.Open(1000m, DateTimeOffset.UtcNow);
        setup.CashSessions.Add(session);
        await setup.SaveChangesAsync();

        // Dos "cajeros" cargan la misma sesión abierta.
        await using var first = db.CreateContext();
        await using var second = db.CreateContext();
        var firstCopy = await first.CashSessions.SingleAsync(s => s.Id == session.Id);
        var secondCopy = await second.CashSessions.SingleAsync(s => s.Id == session.Id);

        firstCopy.Close(1000m, 0m, DateTimeOffset.UtcNow);
        await first.SaveChangesAsync();

        secondCopy.Close(900m, 0m, DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        // Gana el primer cierre.
        await using var verification = db.CreateContext();
        var stored = await verification.CashSessions.SingleAsync(s => s.Id == session.Id);
        Assert.Equal(1000m, stored.CountedAmount);
    }

    private static async Task<CashSessionResponse> OpenAsync(HttpClient client, decimal amount)
    {
        var response = await client.PostAsJsonAsync(BaseUrl, new OpenCashSessionRequest(amount));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CashSessionResponse>())!;
    }

    private static RegisterSaleRequest NewSaleRequest(
        Guid cashSessionId, Guid productId, string method, decimal amount)
        => new(
            Guid.NewGuid(),
            cashSessionId,
            [new SaleProductRequest(productId, 1)],
            [new SalePaymentRequest(method, amount)]);

    private static async Task RegisterSaleAsync(
        HttpClient client, Guid cashSessionId, Guid productId, string method, decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            "/api/sales", NewSaleRequest(cashSessionId, productId, method, amount));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
            var server = Environment.GetEnvironmentVariable("TEST_SQL_SERVER")
                ?? @"(localdb)\MSSQLLocalDB";
            var name = $"CharliesSneakersPOS_{prefix}_{Guid.NewGuid():N}";
            var database = new TestDatabase(
                $"Server={server};Database={name};Trusted_Connection=True;TrustServerCertificate=True");

            await using var context = database.CreateContext();
            await context.Database.MigrateAsync();
            return database;
        }

        public AppDbContext CreateContext() => new(_options);

        public async Task<Guid> SeedProductAsync(int stock)
        {
            var id = Guid.NewGuid();
            await using var context = CreateContext();
            context.ProductVariants.Add(new ProductVariantRecord
            {
                Id = id,
                Sku = "AJ-CASH-HTTP",
                Name = "Air Jordan Caja",
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
