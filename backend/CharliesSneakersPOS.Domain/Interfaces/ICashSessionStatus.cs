
namespace CharliesSneakersPOS.Domain.Interfaces;

public interface ICashSessionStatus
{
    Task<bool> IsOpenAsync(
        Guid cashSessionId,
        CancellationToken cancellationToken = default
    );
}
