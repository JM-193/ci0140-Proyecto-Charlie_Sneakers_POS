
using CharliesSneakersPOS.Domain.Entities;

namespace CharliesSneakersPOS.Tests;

public class SaleItemTests
{
    [Fact]
    public void CreateSaleItem_CalculatesSubtotalCorrectly()
    {
        // Arrange
        var product = new ProductVariant(
            Guid.NewGuid(),
            "AJ-001",
            "Air Jordan 1",
            "42",
            "Black",
            45000m,
            10
        );

        // Act
        var item = new SaleItem(product, 2);

        // Assert
        Assert.Equal(2, item.Quantity);
        Assert.Equal(90000m, item.Subtotal);
    }

    [Fact]
    public void ChangeQuantity_RejectsInsufficientStock()
    {
        // Arrange
        var product = new ProductVariant(
            Guid.NewGuid(),
            "AJ-002",
            "Air Jordan 4",
            "43",
            "Red",
            65000m,
            3
        );

        var item = new SaleItem(product, 1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => item.ChangeQuantity(5)
        );

        Assert.Equal(1, item.Quantity);
    }
}
