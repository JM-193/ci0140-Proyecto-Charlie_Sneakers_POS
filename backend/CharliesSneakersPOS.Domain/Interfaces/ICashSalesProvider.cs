namespace CharliesSneakersPOS.Domain.Interfaces;

// Lo implementa el módulo de ventas.
public interface ICashSalesProvider
{
    // Efectivo neto de la sesión: efectivo recibido menos vuelto entregado.
    Task<decimal> GetNetCashSalesAsync(
        Guid cashSessionId,
        CancellationToken cancellationToken = default);
}
