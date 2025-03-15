using Inventory.Domain.Events;

namespace Inventory.Application.Interfaces;

public interface IEventBus
{
    Task PublishAsync(IDomainEvent domainEvent);
    Task<IEnumerable<IDomainEvent>> ConsumeAsync(CancellationToken cancellationToken);
}
