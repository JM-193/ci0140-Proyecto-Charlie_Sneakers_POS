
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Services;

namespace CharliesSneakersPOS.Tests;

public class SaleCalculatorTests
{
    private readonly SaleCalculator _calculator = new();

    [Fact]
    public void Calculate_ReturnsCorrectSubtotalTaxAndTotal()
    {
        // Arrange
        var items = new[]
        {
            CreateItem(10000m, 1)
        };

        // Act
        var result = _calculator.Calculate(items);

        // Assert
        Assert.Equal(10000m, result.Subtotal);
        Assert.Equal(1300m, result.Tax);
        Assert.Equal(11300m, result.Total);
    }

    [Fact]
    public void Calculate_SumsMultipleProductsCorrectly()
    {
        // Arrange
        var items = new[]
        {
            CreateItem(45000m, 2),
            CreateItem(10000m, 1)
        };

        // Act
        var result = _calculator.Calculate(items);

        // Assert
        Assert.Equal(100000m, result.Subtotal);
        Assert.Equal(13000m, result.Tax);
        Assert.Equal(113000m, result.Total);
    }

    [Fact]
    public void Calculate_EmptyCartReturnsZero()
    {
        // Arrange
        var items = Array.Empty<SaleItem>();

        // Act
        var result = _calculator.Calculate(items);

        // Assert
        Assert.Equal(0m, result.Subtotal);
        Assert.Equal(0m, result.Tax);
        Assert.Equal(0m, result.Total);
    }

    [Fact]
    public void Calculate_RoundsTaxToTwoDecimalPlaces()
    {
        // Arrange
        var items = new[]
        {
            CreateItem(0.05m, 1)
        };

        // Act
        var result = _calculator.Calculate(items);

        // Assert
        Assert.Equal(0.01m, result.Tax);
        Assert.Equal(0.06m, result.Total);
    }

    private static SaleItem CreateItem(
        decimal unitPrice,
        int quantity)
    {
        var product = new ProductVariant(
            Guid.NewGuid(),
            "AJ-TEST",
            "Air Jordan Test",
            "42",
            "Black",
            unitPrice,
            10
        );

        return new SaleItem(product, quantity);
    }
}
