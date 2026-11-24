
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Payments;

namespace CharliesSneakersPOS.Tests;

public class BusinessValidationCoverageTests
{
    [Fact]
    public void RejectsInvalidPaymentMethod()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Payment((PaymentMethod)999, 1000m));
    }

    [Fact]
    public void RejectsPaymentWithMoreThanTwoDecimals()
    {
        Assert.Throws<ArgumentException>(() =>
            new Payment(PaymentMethod.Cash, 1000.123m));
    }

    [Fact]
    public void RejectsProductWithEmptyId()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProductVariant(
                Guid.Empty,
                "AJ-001",
                "Air Jordan",
                "42",
                "Black",
                10000m,
                5));
    }

    [Fact]
    public void RejectsProductWithNegativePrice()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProductVariant(
                Guid.NewGuid(),
                "AJ-001",
                "Air Jordan",
                "42",
                "Black",
                -100m,
                5));
    }

    [Fact]
    public void RejectsProductWithNegativeStock()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProductVariant(
                Guid.NewGuid(),
                "AJ-001",
                "Air Jordan",
                "42",
                "Black",
                10000m,
                -1));
    }

    [Fact]
    public void RejectsProductWithoutSku()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProductVariant(
                Guid.NewGuid(),
                "",
                "Air Jordan",
                "42",
                "Black",
                10000m,
                5));
    }

    [Fact]
    public void RejectsProductWithoutName()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProductVariant(
                Guid.NewGuid(),
                "AJ-001",
                "",
                "42",
                "Black",
                10000m,
                5));
    }
}
