using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Interfaces;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Domain.Services;

namespace CharliesSneakersPOS.Tests;

public class CashClosingTests
{
    // C-02: cierre y cálculo de diferencias de efectivo
    [Fact]
    public async Task CloseAsync_CountMatchesExpected_IsBalanced()
    {
        var (service, repository, session) = await Arrange(50000m, netCashSales: 30000m);

        var summary = await service.CloseAsync(session.Id, 80000m);

        Assert.Equal(80000m, summary.Expected);
        Assert.Equal(0m, summary.Difference);
        Assert.True(summary.IsBalanced);
        Assert.False(session.IsOpen);
        Assert.Equal(80000m, session.ExpectedAmount);
        Assert.Equal(80000m, session.CountedAmount);
        Assert.NotNull(session.ClosedAt);
        Assert.Equal(2, repository.SaveCount);
    }

    [Fact]
    public async Task CloseAsync_CountAboveExpected_ReportsSurplus()
    {
        var (service, _, session) = await Arrange(50000m, netCashSales: 30000m);

        var summary = await service.CloseAsync(session.Id, 80500m);

        Assert.Equal(500m, summary.Surplus);
        Assert.Equal(0m, summary.Shortage);
        Assert.False(summary.IsBalanced);
    }

    [Fact]
    public async Task CloseAsync_CountBelowExpected_ReportsShortageAndStillCloses()
    {
        var (service, _, session) = await Arrange(50000m, netCashSales: 30000m);

        var summary = await service.CloseAsync(session.Id, 79000m);

        Assert.Equal(1000m, summary.Shortage);
        Assert.Equal(0m, summary.Surplus);
        Assert.False(session.IsOpen);
    }

    [Fact]
    public async Task CloseAsync_NoSales_ExpectedEqualsOpeningAmount()
    {
        var (service, _, session) = await Arrange(50000m, netCashSales: 0m);

        var summary = await service.CloseAsync(session.Id, 50000m);

        Assert.Equal(50000m, summary.Expected);
        Assert.True(summary.IsBalanced);
    }

    [Fact]
    public async Task CloseAsync_NegativeCount_IsRejectedAndSessionStaysOpen()
    {
        var (service, _, session) = await Arrange(50000m, netCashSales: 0m);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CloseAsync(session.Id, -1m));

        Assert.True(session.IsOpen);
    }

    [Fact]
    public async Task CloseAsync_MoreThanTwoDecimals_IsRejected()
    {
        var (service, _, session) = await Arrange(50000m, netCashSales: 0m);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CloseAsync(session.Id, 100.123m));

        Assert.True(session.IsOpen);
    }

    [Fact]
    public async Task CloseAsync_UnknownSession_Throws()
    {
        var (service, _, _) = await Arrange(50000m, netCashSales: 0m);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CloseAsync(Guid.NewGuid(), 100m));
    }

    [Fact]
    public void Constructor_NullDependencies_Throw()
    {
        var repository = new FakeCashSessionRepository();
        var provider = new FakeCashSalesProvider(0m);

        Assert.Throws<ArgumentNullException>(() => new CashClosingService(null!, provider));
        Assert.Throws<ArgumentNullException>(() => new CashClosingService(repository, null!));
    }

    // C-03: cierres duplicados y bloqueo de ventas con caja cerrada
    [Fact]
    public async Task CloseAsync_AlreadyClosed_IsRejectedAndKeepsFirstResult()
    {
        var (service, repository, session) = await Arrange(50000m, netCashSales: 30000m);
        await service.CloseAsync(session.Id, 80000m);
        var savesAfterFirstClose = repository.SaveCount;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CloseAsync(session.Id, 1m));

        Assert.Equal(80000m, session.CountedAmount);
        Assert.Equal(savesAfterFirstClose, repository.SaveCount);
    }

    [Fact]
    public async Task CompleteAsync_AfterCashSessionClosed_RejectsSale()
    {
        var (closingService, repository, session) = await Arrange(50000m, netCashSales: 0m);
        var saleService = new SaleCompletionService(repository);
        await closingService.CloseAsync(session.Id, 50000m);

        var sale = CreateSale();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => saleService.CompleteAsync(
                sale,
                session.Id,
                new[] { new Payment(PaymentMethod.Cash, 11300m) }));

        Assert.False(sale.IsCompleted);
    }

    [Fact]
    public async Task CompleteAsync_WhileCashSessionOpen_CompletesSale()
    {
        var (_, repository, session) = await Arrange(50000m, netCashSales: 0m);
        var saleService = new SaleCompletionService(repository);

        var sale = CreateSale();

        await saleService.CompleteAsync(
            sale,
            session.Id,
            new[] { new Payment(PaymentMethod.Cash, 11300m) });

        Assert.True(sale.IsCompleted);
    }

    private static async Task<(CashClosingService Service, FakeCashSessionRepository Repository, CashSession Session)>
        Arrange(decimal openingAmount, decimal netCashSales)
    {
        var repository = new FakeCashSessionRepository();
        var session = await new CashSessionService(repository).OpenAsync(openingAmount);
        var service = new CashClosingService(repository, new FakeCashSalesProvider(netCashSales));
        return (service, repository, session);
    }

    private static Sale CreateSale()
    {
        var product = new ProductVariant(
            Guid.NewGuid(), "AJ-001", "Air Jordan 1", "42", "Black", 10000m, 10);

        var sale = new Sale();
        sale.AddItem(product, 1);
        return sale;
    }

    private sealed class FakeCashSalesProvider : ICashSalesProvider
    {
        private readonly decimal _netCashSales;

        public FakeCashSalesProvider(decimal netCashSales) => _netCashSales = netCashSales;

        public Task<decimal> GetNetCashSalesAsync(
            Guid cashSessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(_netCashSales);
    }
}
