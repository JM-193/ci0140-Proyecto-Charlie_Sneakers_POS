using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Interfaces;
using CharliesSneakersPOS.Domain.Services;

namespace CharliesSneakersPOS.Tests;

public class CashSessionTests
{
    // C-01: apertura de caja
    [Fact]
    public async Task OpenAsync_ValidAmount_OpensSession()
    {
        var repository = new FakeCashSessionRepository();
        var service = new CashSessionService(repository);

        var session = await service.OpenAsync(50000m);

        Assert.True(session.IsOpen);
        Assert.Equal(50000m, session.OpeningAmount);
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Single(repository.Sessions);
        Assert.Equal(1, repository.SaveCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task OpenAsync_NonPositiveAmount_IsRejected(int amount)
    {
        var repository = new FakeCashSessionRepository();
        var service = new CashSessionService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.OpenAsync(amount));

        Assert.Empty(repository.Sessions);
    }

    [Fact]
    public async Task OpenAsync_MoreThanTwoDecimals_IsRejected()
    {
        var service = new CashSessionService(new FakeCashSessionRepository());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.OpenAsync(100.123m));
    }

    [Fact]
    public async Task OpenAsync_AlreadyOpenSession_IsRejected()
    {
        var repository = new FakeCashSessionRepository();
        var service = new CashSessionService(repository);
        await service.OpenAsync(50000m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.OpenAsync(10000m));

        Assert.Single(repository.Sessions);
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CashSessionService(null!));
    }
}
