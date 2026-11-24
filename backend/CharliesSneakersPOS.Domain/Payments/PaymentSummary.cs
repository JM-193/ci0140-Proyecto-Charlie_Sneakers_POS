
namespace CharliesSneakersPOS.Domain.Payments;

public sealed record PaymentSummary(
    decimal Total,
    decimal TotalReceived,
    decimal Remaining,
    decimal Change
)
{
    public bool IsFullyPaid => Remaining == 0m;
}
