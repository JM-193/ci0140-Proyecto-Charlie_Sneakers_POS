
using CharliesSneakersPOS.Domain.Entities;

namespace CharliesSneakersPOS.Tests;

public class SaleTests
{
    [Fact]
    public void AddItem_AddsProductAndCalculatesTotals()
    {
        var sale = new Sale();
        var product = CreateProduct(10000m, 5);

        sale.AddItem(product, 2);

        Assert.Single(sale.Items);
        Assert.Equal(20000m, sale.Totals.Subtotal);
        Assert.Equal(2600m, sale.Totals.Tax);
        Assert.Equal(22600m, sale.Totals.Total);
    }

    [Fact]
    public void AddItem_SameProductIncreasesQuantity()
    {
        var sale = new Sale();
        var product = CreateProduct(10000m, 5);

        sale.AddItem(product, 1);
        sale.AddItem(product, 2);

        Assert.Single(sale.Items);
        Assert.Equal(3, sale.Items[0].Quantity);
        Assert.Equal(30000m, sale.Totals.Subtotal);
    }

    [Fact]
    public void ChangeQuantity_RecalculatesTotals()
    {
        var sale = new Sale();
        var product = CreateProduct(10000m, 5);

        sale.AddItem(product, 1);
        sale.ChangeQuantity(product.Id, 3);

        Assert.Equal(3, sale.Items[0].Quantity);
        Assert.Equal(30000m, sale.Totals.Subtotal);
        Assert.Equal(33900m, sale.Totals.Total);
    }

    [Fact]
    public void RemoveItem_UpdatesTotals()
    {
        var sale = new Sale();

        var firstProduct = CreateProduct(10000m, 5);
        var secondProduct = CreateProduct(20000m, 5);

        sale.AddItem(firstProduct, 1);
        sale.AddItem(secondProduct, 1);

        sale.RemoveItem(firstProduct.Id);

        Assert.Single(sale.Items);
        Assert.Equal(20000m, sale.Totals.Subtotal);
        Assert.Equal(22600m, sale.Totals.Total);
    }

    [Fact]
    public void AddItem_RejectsInsufficientStock()
    {
        var sale = new Sale();
        var product = CreateProduct(10000m, 3);

        sale.AddItem(product, 2);

        Assert.Throws<InvalidOperationException>(
            () => sale.AddItem(product, 2)
        );

        Assert.Equal(2, sale.Items[0].Quantity);
    }

    [Fact]
    public void RemoveItem_RejectsUnknownProduct()
    {
        var sale = new Sale();

        Assert.Throws<KeyNotFoundException>(
            () => sale.RemoveItem(Guid.NewGuid())
        );
    }

    private static ProductVariant CreateProduct(
        decimal price,
        int stock)
    {
        return new ProductVariant(
            Guid.NewGuid(),
            "AJ-TEST",
            "Air Jordan Test",
            "42",
            "Black",
            price,
            stock
        );
    }
}
