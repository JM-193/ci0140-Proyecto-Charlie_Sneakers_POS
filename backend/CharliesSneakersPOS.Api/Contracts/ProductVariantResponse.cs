
namespace CharliesSneakersPOS.Api.Contracts;

public sealed record ProductVariantResponse(
    Guid Id,
    string Sku,
    string Name,
    string? Barcode,
    string Size,
    string Color,
    decimal UnitPrice,
    int Stock
);
