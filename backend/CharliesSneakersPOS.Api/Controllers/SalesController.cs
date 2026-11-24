
using CharliesSneakersPOS.Api.Contracts;
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharliesSneakersPOS.Api.Controllers;

[ApiController]
[Route("api/sales")]
public sealed class SalesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SaleRegistrationService _registration;

    public SalesController(
        AppDbContext db,
        SaleRegistrationService registration)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));

        _registration = registration
            ?? throw new ArgumentNullException(nameof(registration));
    }

    [HttpPost]
    public async Task<ActionResult<RegisterSaleResponse>> Register(
        [FromBody] RegisterSaleRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null ||
            request.SaleId == Guid.Empty ||
            request.CashSessionId == Guid.Empty ||
            request.Items is null ||
            request.Items.Count is 0 or > 50 ||
            request.Payments is null ||
            request.Payments.Count is 0 or > 10)
        {
            return BadRequest("Invalid sale request.");
        }

        try
        {
            var sale = await BuildSaleAsync(
                request.Items,
                cancellationToken);

            var payments = BuildPayments(request.Payments);

            await _registration.RegisterAsync(
                request.SaleId,
                request.CashSessionId,
                sale,
                payments,
                cancellationToken);

            var totals = sale.Totals;

            return Ok(new RegisterSaleResponse(
                request.SaleId,
                totals.Subtotal,
                totals.Tax,
                totals.Total,
                sale.PaymentSummary!.Change));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private async Task<Sale> BuildSaleAsync(
        IReadOnlyList<SaleProductRequest> items,
        CancellationToken cancellationToken)
    {
        var productIds = items
            .Select(i => i.ProductVariantId)
            .Distinct()
            .ToArray();

        var products = await _db.ProductVariants
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(
                p => p.Id,
                cancellationToken);

        var sale = new Sale();

        foreach (var requested in items)
        {
            if (!products.TryGetValue(
                requested.ProductVariantId,
                out var product))
            {
                throw new KeyNotFoundException(
                    "A requested product was not found.");
            }

            var variant = new ProductVariant(
                product.Id,
                product.Sku,
                product.Name,
                product.Size,
                product.Color,
                product.UnitPrice,
                product.Stock);

            sale.AddItem(variant, requested.Quantity);
        }

        return sale;
    }

    private static List<Payment> BuildPayments(
        IReadOnlyList<SalePaymentRequest> payments)
    {
        var result = new List<Payment>();

        foreach (var payment in payments)
        {
            var methodName = payment.Method?.Trim();

            if (!Enum.GetNames<PaymentMethod>()
                .Contains(
                    methodName,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Invalid payment method.");
            }

            var method = Enum.Parse<PaymentMethod>(
                methodName!,
                ignoreCase: true);

            result.Add(new Payment(method, payment.Amount));
        }

        return result;
    }
}
