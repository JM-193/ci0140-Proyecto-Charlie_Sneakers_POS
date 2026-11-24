
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Models;

namespace CharliesSneakersPOS.Domain.Services;

public sealed class SaleCalculator
{
    private const decimal TaxRate = 0.13m;

    public SaleTotals Calculate(IEnumerable<SaleItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        decimal subtotal = 0m;

        foreach (var item in items)
        {
            subtotal += item.Subtotal;
        }

        decimal tax = Math.Round(
            subtotal * TaxRate,
            2,
            MidpointRounding.AwayFromZero
        );

        decimal total = subtotal + tax;

        return new SaleTotals(subtotal, tax, total);
    }
}
