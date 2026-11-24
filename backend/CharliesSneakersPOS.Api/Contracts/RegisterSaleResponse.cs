
namespace CharliesSneakersPOS.Api.Contracts;

public sealed record RegisterSaleResponse(
    Guid SaleId,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    decimal Change
);
