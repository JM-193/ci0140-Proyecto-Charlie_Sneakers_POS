
namespace CharliesSneakersPOS.Domain.Models;

public sealed record SaleTotals(
    decimal Subtotal,
    decimal Tax,
    decimal Total
);
