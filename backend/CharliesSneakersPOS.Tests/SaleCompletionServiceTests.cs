
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Interfaces;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Domain.Services;

namespace CharliesSneakersPOS.Tests;

public class SaleCompletionServiceTests
{
    [Fact]
    public async Task CompleteAsync_OpenCashSession_CompletesSale()
    {
        var service = new SaleCompletionService(
            new FakeCashSessionStatus(true));

        var sale = CreateSale();

        await service.CompleteAsync(
            sale,
            Guid.NewGuid(),
            new[] { new Payment(PaymentMethod.Cash, 11300m) });

        Assert.True(sale.IsCompleted);
        Assert.Single(sale.Payments);
    }

    [Fact]
    public async Task CompleteAsync_ClosedCashSession_RejectsSale()
    {
        var service = new SaleCompletionService(
            new FakeCashSessionStatus(false));

        var sale = CreateSale();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CompleteAsync(
                sale,
                Guid.NewGuid(),
                new[] { new Payment(PaymentMethod.Cash, 11300m) }));

        Assert.False(sale.IsCompleted);
        Assert.Empty(sale.Payments);
    }

    [Fact]
    public async Task CompleteAsync_InvalidSessionId_RejectsSale()
    {
        var service = new SaleCompletionService(
            new FakeCashSessionStatus(true));

        var sale = CreateSale();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CompleteAsync(
                sale,
                Guid.Empty,
                new[] { new Payment(PaymentMethod.Cash, 11300m) }));

        Assert.False(sale.IsCompleted);
    }

    private static Sale CreateSale()
    {
        var product = new ProductVariant(
            Guid.NewGuid(),
            "AJ-001",
            "Air Jordan 1",
            "42",
            "Black",
            10000m,
            10
        );

        var sale = new Sale();
        sale.AddItem(product, 1);

        return sale;
    }

    private sealed class FakeCashSessionStatus
        : ICashSessionStatus
    {
        private readonly bool _isOpen;

        public FakeCashSessionStatus(bool isOpen)
        {
            _isOpen = isOpen;
        }

        public Task<bool> IsOpenAsync(
            Guid cashSessionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_isOpen);
        }
    }
}
