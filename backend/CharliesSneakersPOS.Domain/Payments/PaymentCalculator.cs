
namespace CharliesSneakersPOS.Domain.Payments;

public sealed class PaymentCalculator
{
    public PaymentSummary Calculate(
        decimal total,
        IEnumerable<Payment> payments)
    {
        ArgumentNullException.ThrowIfNull(payments);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(total);

        decimal remaining = total;
        decimal received = 0m;
        decimal change = 0m;

        foreach (var payment in payments)
        {
            if (payment is null)
                throw new ArgumentException("Invalid payment.", nameof(payments));

            if (remaining == 0m)
                throw new InvalidOperationException(
                    "The sale is already fully paid.");

            // Solo el efectivo permite pagar más del saldo pendiente.
            if (payment.Method != PaymentMethod.Cash &&
                payment.Amount > remaining)
            {
                throw new InvalidOperationException(
                    "Non-cash payment exceeds the remaining amount.");
            }

            received += payment.Amount;

            if (payment.Amount > remaining)
            {
                change = payment.Amount - remaining;
                remaining = 0m;
            }
            else
            {
                remaining -= payment.Amount;
            }
        }

        return new PaymentSummary(
            total,
            received,
            remaining,
            change
        );
    }
}
