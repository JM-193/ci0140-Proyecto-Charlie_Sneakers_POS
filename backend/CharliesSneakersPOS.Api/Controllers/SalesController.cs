
using CharliesSneakersPOS.Api.Contracts;
using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

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
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                error = "The product was modified by another sale."
            });
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException
            { Number: 1205 or 3960 or 2601 or 2627 })
        {
            return Conflict(new
            {
                error = "The sale conflicts with another operation."
            });
        }
        catch (SqlException ex)
            when (ex.Number is 1205 or 3960 or 2601 or 2627)
        {
            return Conflict(new
            {
                error = "The sale conflicts with another operation."
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // HU-03 / HU-05: recalculates totals, remaining balance and change with
    // the same sale-building logic as Register, without persisting anything.
    [HttpPost("preview")]
    public async Task<ActionResult<PreviewSaleResponse>> Preview(
        [FromBody] PreviewSaleRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null ||
            request.Items is null ||
            request.Items.Count is 0 or > 50 ||
            request.Payments?.Count > 10)
        {
            return BadRequest(new PreviewSaleError(
                "Invalid sale preview request.",
                PreviewSaleErrorCodes.InvalidRequest));
        }

        Sale sale;

        try
        {
            sale = await BuildSaleAsync(
                request.Items,
                cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new PreviewSaleError(
                ex.Message,
                PreviewSaleErrorCodes.ProductNotFound));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new PreviewSaleError(
                ex.Message,
                PreviewSaleErrorCodes.InvalidQuantity));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new PreviewSaleError(
                ex.Message,
                PreviewSaleErrorCodes.InsufficientStock));
        }

        return PreviewPayments(sale, request.Payments);
    }

    private ActionResult<PreviewSaleResponse> PreviewPayments(
        Sale sale,
        IReadOnlyList<SalePaymentRequest>? requestedPayments)
    {
        var totals = sale.Totals;

        if (requestedPayments is null || requestedPayments.Count == 0)
        {
            return Ok(new PreviewSaleResponse(
                totals.Subtotal,
                totals.Tax,
                totals.Total,
                0m,
                totals.Total,
                0m));
        }

        try
        {
            var summary = sale.PreviewPayments(
                BuildPayments(requestedPayments));

            return Ok(new PreviewSaleResponse(
                totals.Subtotal,
                totals.Tax,
                totals.Total,
                summary.TotalReceived,
                summary.Remaining,
                summary.Change));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new PreviewSaleError(
                ex.Message,
                PreviewSaleErrorCodes.InvalidPayment));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new PreviewSaleError(
                ex.Message,
                PreviewSaleErrorCodes.PaymentNotAllowed));
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
