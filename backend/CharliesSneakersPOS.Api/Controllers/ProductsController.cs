
using CharliesSneakersPOS.Api.Contracts;
using CharliesSneakersPOS.Infrastructure.Persistence;
using CharliesSneakersPOS.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharliesSneakersPOS.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductVariantResponse>>> Get(
        [FromQuery] string? q,
        [FromQuery] string? size,
        [FromQuery] string? color,
        CancellationToken cancellationToken)
    {
        if (HasInvalidSearchParameters(q, size, color))
        {
            return BadRequest("Search parameters are too long.");
        }

        var products = ApplyFilters(
            _db.ProductVariants.AsNoTracking(),
            q,
            size,
            color);

        var result = await products
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Sku)
            .Take(50)
            .Select(p => new ProductVariantResponse(
                p.Id,
                p.Sku,
                p.Name,
                p.Barcode,
                p.Size,
                p.Color,
                p.UnitPrice,
                p.Stock))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    // Validate search parameter lengths.
    private static bool HasInvalidSearchParameters(
        string? q,
        string? size,
        string? color)
    {
        return q?.Length > 100 ||
               size?.Length > 30 ||
               color?.Length > 80;
    }

    // Build the query using the optional filters.
    private static IQueryable<ProductVariantRecord> ApplyFilters(
        IQueryable<ProductVariantRecord> products,
        string? q,
        string? size,
        string? color)
    {
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();

            products = products.Where(p =>
                p.Name.Contains(term) ||
                p.Sku.Contains(term) ||
                (p.Barcode != null && p.Barcode.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(size))
        {
            var selectedSize = size.Trim();

            products = products.Where(p =>
                p.Size == selectedSize);
        }

        if (!string.IsNullOrWhiteSpace(color))
        {
            var selectedColor = color.Trim();

            products = products.Where(p =>
                p.Color == selectedColor);
        }

        return products;
    }
}
