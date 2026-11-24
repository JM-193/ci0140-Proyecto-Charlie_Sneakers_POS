using CharliesSneakersPOS.Domain.Interfaces;

namespace CharliesSneakersPOS.Tests;

internal sealed class FakeCashSalesProvider : ICashSalesProvider
{
    private readonly decimal _netCashSales;

    public FakeCashSalesProvider(decimal netCashSales) => _netCashSales = netCashSales;

    public Task<decimal> GetNetCashSalesAsync(
        Guid cashSessionId, CancellationToken cancellationToken = default)
        => Task.FromResult(_netCashSales);
}
