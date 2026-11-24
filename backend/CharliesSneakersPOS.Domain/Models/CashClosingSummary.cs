namespace CharliesSneakersPOS.Domain.Models;

// Resultado del cierre. Difference = Counted - Expected.
public sealed record CashClosingSummary(decimal Expected, decimal Counted)
{
    public decimal Difference => Counted - Expected;
    public decimal Surplus => Difference > 0 ? Difference : 0m;
    public decimal Shortage => Difference < 0 ? -Difference : 0m;
    public bool IsBalanced => Difference == 0m;
}
