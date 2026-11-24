using CharliesSneakersPOS.Domain.Models;

namespace CharliesSneakersPOS.Domain.Entities;

public sealed class CashSession
{
    // Monto de apertura mínimo permitido (debe ser mayor a cero).
    public const decimal MinimumOpeningAmount = 0.01m;

    public Guid Id { get; private set; }
    public decimal OpeningAmount { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public CashSessionStatus Status { get; private set; }

    // Requerido por EF Core.
    private CashSession() { }

    private CashSession(decimal openingAmount, DateTimeOffset openedAt)
    {
        Id = Guid.NewGuid();
        OpeningAmount = openingAmount;
        OpenedAt = openedAt;
        Status = CashSessionStatus.Open;
    }

    public DateTimeOffset? ClosedAt { get; private set; }
    public decimal? ExpectedAmount { get; private set; }
    public decimal? CountedAmount { get; private set; }

    public bool IsOpen => Status == CashSessionStatus.Open;

    // Efectivo esperado = apertura + efectivo neto de ventas (recibido - vuelto).
    public CashClosingSummary Close(
        decimal countedAmount,
        decimal netCashSales,
        DateTimeOffset closedAt)
    {
        if (!IsOpen)
            throw new InvalidOperationException("The cash session is already closed.");

        if (countedAmount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(countedAmount),
                "Counted amount cannot be negative.");

        if (decimal.Round(countedAmount, 2) != countedAmount)
            throw new ArgumentException(
                "Counted amount cannot have more than two decimal places.",
                nameof(countedAmount));

        ExpectedAmount = OpeningAmount + netCashSales;
        CountedAmount = countedAmount;
        ClosedAt = closedAt;
        Status = CashSessionStatus.Closed;

        return new CashClosingSummary(ExpectedAmount.Value, countedAmount);
    }

    public static CashSession Open(decimal openingAmount, DateTimeOffset openedAt)
    {
        if (openingAmount < MinimumOpeningAmount)
            throw new ArgumentOutOfRangeException(
                nameof(openingAmount),
                "Opening amount must be greater than zero.");

        if (decimal.Round(openingAmount, 2) != openingAmount)
            throw new ArgumentException(
                "Opening amount cannot have more than two decimal places.",
                nameof(openingAmount));

        return new CashSession(openingAmount, openedAt);
    }
}
