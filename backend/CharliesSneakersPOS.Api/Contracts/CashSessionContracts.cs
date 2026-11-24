using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Models;

namespace CharliesSneakersPOS.Api.Contracts;

public sealed record OpenCashSessionRequest(decimal OpeningAmount);

public sealed record CloseCashSessionRequest(decimal CountedAmount);

public sealed record CashSessionResponse(
    Guid Id,
    string Status,
    decimal OpeningAmount,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal? ExpectedAmount,
    decimal? CountedAmount)
{
    public static CashSessionResponse From(CashSession session) => new(
        session.Id,
        session.Status.ToString(),
        session.OpeningAmount,
        session.OpenedAt,
        session.ClosedAt,
        session.ExpectedAmount,
        session.CountedAmount);
}

public sealed record CashClosingResponse(
    Guid CashSessionId,
    decimal Expected,
    decimal Counted,
    decimal Difference,
    decimal Surplus,
    decimal Shortage,
    bool IsBalanced)
{
    public static CashClosingResponse From(Guid cashSessionId, CashClosingSummary summary) => new(
        cashSessionId,
        summary.Expected,
        summary.Counted,
        summary.Difference,
        summary.Surplus,
        summary.Shortage,
        summary.IsBalanced);
}
