
namespace CharliesSneakersPOS.Infrastructure.Persistence.Entities;

public sealed class SaleRecord
{
    public Guid Id { get; set; }

    public Guid CashSessionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public decimal Change { get; set; }

    public List<SaleItemRecord> Items { get; set; } = new();

    public List<SalePaymentRecord> Payments { get; set; } = new();
}
