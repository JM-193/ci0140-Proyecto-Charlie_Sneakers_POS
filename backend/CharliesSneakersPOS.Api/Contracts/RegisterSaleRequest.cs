
namespace CharliesSneakersPOS.Api.Contracts;

public sealed record SaleProductRequest(
    Guid ProductVariantId,
    int Quantity
);

public sealed record SalePaymentRequest(
    string Method,
    decimal Amount
);

public sealed record RegisterSaleRequest(
    Guid SaleId,
    Guid CashSessionId,
    IReadOnlyList<SaleProductRequest> Items,
    IReadOnlyList<SalePaymentRequest> Payments
);
