using CharliesSneakersPOS.Domain.Entities;
using CharliesSneakersPOS.Domain.Payments;
using CharliesSneakersPOS.Domain.Services;

namespace CharliesSneakersPOS.Tests;

// Escenarios C-01, C-02 y C-03 de CU-02 (Cerrar y cuadrar caja) tal como se
// especifican en el informe. Todos usan las mismas operaciones del turno:
// monto inicial 20 000, efectivo recibido 45 000 y vuelto entregado 5 000,
// por lo que el monto esperado es 20 000 + 45 000 - 5 000 = 60 000.
public class CashClosingReportScenarioTests
{
    private const decimal Initial = 20000m;
    private const decimal CashReceived = 45000m;
    private const decimal ChangeGiven = 5000m;
    private const decimal Expected = 60000m;

    [Fact]
    public async Task C01_ExactCount_ClosesCashWithZeroDifference()
    {
        var (service, _, session) = await ArrangeShiftAsync();

        var summary = await service.CloseAsync(session.Id, 60000m);

        Assert.Equal(Expected, summary.Expected);
        Assert.Equal(0m, summary.Difference);
        Assert.True(summary.IsBalanced);
        Assert.Equal(CashSessionStatus.Closed, session.Status);
    }

    [Fact]
    public async Task C02_CountAboveExpected_ReportsSurplusOf3000()
    {
        var (service, _, session) = await ArrangeShiftAsync();

        var summary = await service.CloseAsync(session.Id, 63000m);

        Assert.Equal(3000m, summary.Surplus);
        Assert.Equal(0m, summary.Shortage);
        Assert.Equal(3000m, summary.Difference);
        Assert.Equal(CashSessionStatus.Closed, session.Status);
    }

    [Fact]
    public async Task C03_CountBelowExpected_ReportsShortageAndRejectsLaterSales()
    {
        var (service, repository, session) = await ArrangeShiftAsync();

        var summary = await service.CloseAsync(session.Id, 58000m);

        Assert.Equal(2000m, summary.Shortage);
        Assert.Equal(0m, summary.Surplus);
        Assert.Equal(CashSessionStatus.Closed, session.Status);

        // Un intento posterior de venta con la caja cerrada se rechaza.
        var sale = CreateSale();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SaleCompletionService(repository).CompleteAsync(
                sale,
                session.Id,
                new[] { new Payment(PaymentMethod.Cash, 11300m) }));
        Assert.False(sale.IsCompleted);
    }

    private static async Task<(CashClosingService Service, FakeCashSessionRepository Repository, CashSession Session)>
        ArrangeShiftAsync()
    {
        var repository = new FakeCashSessionRepository();
        var session = await new CashSessionService(repository).OpenAsync(Initial);

        // Efectivo neto de ventas = recibido - vuelto.
        var service = new CashClosingService(
            repository,
            new FakeCashSalesProvider(CashReceived - ChangeGiven));

        return (service, repository, session);
    }

    private static Sale CreateSale()
    {
        var product = new ProductVariant(
            Guid.NewGuid(), "AJ-001", "Air Jordan 1", "42", "Black", 10000m, 10);

        var sale = new Sale();
        sale.AddItem(product, 1);
        return sale;
    }
}
