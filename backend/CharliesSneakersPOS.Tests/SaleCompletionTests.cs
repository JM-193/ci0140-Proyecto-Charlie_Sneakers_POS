
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Payments;

namespace CharliesSneakersPOS.Tests;

public class SaleCompletionTests
{
    [Fact]
    public void Complete_ExactCashPayment_CompletesSale()
    {
        var (sale, _) = CreateSale();

        sale.Complete(new[]
        {
            new Payment(PaymentMethod.Cash, 11300m)
        });

        Assert.True(sale.IsCompleted);
        Assert.Single(sale.Payments);
        Assert.NotNull(sale.PaymentSummary);
        Assert.Equal(0m, sale.PaymentSummary.Remaining);
        Assert.Equal(0m, sale.PaymentSummary.Change);
    }

    [Fact]
    public void Complete_MixedPayment_CalculatesChange()
    {
        var (sale, _) = CreateSale();

        sale.Complete(new[]
        {
            new Payment(PaymentMethod.Card, 5000m),
            new Payment(PaymentMethod.Cash, 10000m)
        });

        Assert.True(sale.IsCompleted);
        Assert.Equal(2, sale.Payments.Count);
        Assert.Equal(3700m, sale.PaymentSummary!.Change);
    }

    [Fact]
    public void PreviewPayments_PartialPayment_ShowsRemaining()
    {
        var (sale, _) = CreateSale();

        var result = sale.PreviewPayments(new[]
        {
            new Payment(PaymentMethod.SinpeMobile, 5000m)
        });

        Assert.Equal(6300m, result.Remaining);
        Assert.False(result.IsFullyPaid);
        Assert.False(sale.IsCompleted);
    }

    [Fact]
    public void Complete_InsufficientPayment_IsRejected()
    {
        var (sale, _) = CreateSale();

        Assert.Throws<InvalidOperationException>(() =>
            sale.Complete(new[]
            {
                new Payment(PaymentMethod.Cash, 5000m)
            })
        );

        Assert.False(sale.IsCompleted);
        Assert.Empty(sale.Payments);
    }

    [Fact]
    public void Complete_CardOverpayment_IsRejected()
    {
        var (sale, _) = CreateSale();

        Assert.Throws<InvalidOperationException>(() =>
            sale.Complete(new[]
            {
                new Payment(PaymentMethod.Card, 12000m)
            })
        );

        Assert.False(sale.IsCompleted);
        Assert.Empty(sale.Payments);
    }

    [Fact]
    public void Complete_EmptyCart_IsRejected()
    {
        var sale = new Sale();

        Assert.Throws<InvalidOperationException>(() =>
            sale.Complete(new[]
            {
                new Payment(PaymentMethod.Cash, 1000m)
            })
        );

        Assert.False(sale.IsCompleted);
    }

    [Fact]
    public void CompletedSale_CannotBeModified()
    {
        var (sale, product) = CreateSale();

        sale.Complete(new[]
        {
            new Payment(PaymentMethod.Cash, 11300m)
        });

        Assert.Throws<InvalidOperationException>(() =>
            sale.AddItem(product, 1));

        Assert.Throws<InvalidOperationException>(() =>
            sale.ChangeQuantity(product.Id, 2));

        Assert.Throws<InvalidOperationException>(() =>
            sale.RemoveItem(product.Id));

        Assert.Throws<InvalidOperationException>(() =>
            sale.Items[0].ChangeQuantity(2));
    }

    [Fact]
    public void Complete_Twice_IsRejected()
    {
        var (sale, _) = CreateSale();

        var payments = new[]
        {
            new Payment(PaymentMethod.Cash, 11300m)
        };

        sale.Complete(payments);

        Assert.Throws<InvalidOperationException>(() =>
            sale.Complete(payments));

        Assert.Single(sale.Payments);
    }

    private static (Sale Sale, ProductVariant Product) CreateSale()
    {
        var product = new ProductVariant(
            Guid.NewGuid(),
            "AJ-001",
            "Air Jordan 1",
            "42",
            "Black",
            10000m,
            10
        );

        var sale = new Sale();
        sale.AddItem(product, 1);

        return (sale, product);
    }
}
