
using CharliesSneakersPOS.Domain.Payments;

namespace CharliesSneakersPOS.Infrastructure.Persistence.Entities;

public sealed class SalePaymentRecord
{
    public Guid Id { get; set; }

    public Guid SaleId { get; set; }

    public PaymentMethod Method { get; set; }

    public decimal Amount { get; set; }
}
