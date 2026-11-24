
namespace CharliesSneakersPOS.Api.Contracts;

// HU-03 / HU-05: totals and payment status calculated by the backend
// without persisting the sale. Payments are optional.
public sealed record PreviewSaleRequest(
    IReadOnlyList<SaleProductRequest> Items,
    IReadOnlyList<SalePaymentRequest>? Payments
);

public sealed record PreviewSaleResponse(
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    decimal TotalReceived,
    decimal Remaining,
    decimal Change
);

// Error body with a stable, machine-readable code so the UI can choose
// its own message without depending on the English text.
public sealed record PreviewSaleError(
    string Error,
    string Code
);

public static class PreviewSaleErrorCodes
{
    public const string InvalidRequest = "invalid_request";
    public const string ProductNotFound = "product_not_found";
    public const string InvalidQuantity = "invalid_quantity";
    public const string InsufficientStock = "insufficient_stock";
    public const string InvalidPayment = "invalid_payment";
    public const string PaymentNotAllowed = "payment_not_allowed";
}
