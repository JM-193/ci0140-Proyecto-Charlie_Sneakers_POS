
using CharliesSneakersPOS.Domain.Payments;

namespace CharliesSneakersPOS.Tests;

public class PaymentCalculatorTests
{
    private readonly PaymentCalculator _calculator = new();

    [Fact]
    public void CashPayment_ExactAmount_CompletesSale()
    {
        var payments = new[]
        {
            new Payment(PaymentMethod.Cash, 11300m)
        };

        var result = _calculator.Calculate(11300m, payments);

        Assert.True(result.IsFullyPaid);
        Assert.Equal(0m, result.Remaining);
        Assert.Equal(0m, result.Change);
    }

    [Fact]
    public void CashPayment_Overpayment_CalculatesChange()
    {
        var payments = new[]
        {
            new Payment(PaymentMethod.Cash, 15000m)
        };

        var result = _calculator.Calculate(11300m, payments);

        Assert.True(result.IsFullyPaid);
        Assert.Equal(3700m, result.Change);
    }

    [Fact]
    public void MixedPayment_CalculatesChangeCorrectly()
    {
        var payments = new[]
        {
            new Payment(PaymentMethod.Card, 5000m),
            new Payment(PaymentMethod.Cash, 10000m)
        };

        var result = _calculator.Calculate(11300m, payments);

        Assert.True(result.IsFullyPaid);
        Assert.Equal(15000m, result.TotalReceived);
        Assert.Equal(3700m, result.Change);
    }

    [Fact]
    public void PartialPayment_ShowsRemainingAmount()
    {
        var payments = new[]
        {
            new Payment(PaymentMethod.SinpeMobile, 5000m)
        };

        var result = _calculator.Calculate(11300m, payments);

        Assert.False(result.IsFullyPaid);
        Assert.Equal(6300m, result.Remaining);
    }

    [Fact]
    public void CardPayment_Overpayment_IsRejected()
    {
        var payments = new[]
        {
            new Payment(PaymentMethod.Card, 12000m)
        };

        Assert.Throws<InvalidOperationException>(
            () => _calculator.Calculate(11300m, payments)
        );
    }

    [Fact]
    public void Payment_ZeroAmount_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Payment(PaymentMethod.Cash, 0m)
        );
    }
}
