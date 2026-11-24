
namespace CharliesSneakersPOS.Domain.Payments;

public sealed class Payment
{
    public PaymentMethod Method { get; }
    public decimal Amount { get; }

    public Payment(PaymentMethod method, decimal amount)
    {
        if (!Enum.IsDefined(method))
            throw new ArgumentOutOfRangeException(nameof(method));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount must be greater than zero.");

        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException(
                "Payment amount cannot have more than two decimal places.",
                nameof(amount));

        Method = method;
        Amount = amount;
    }
}
