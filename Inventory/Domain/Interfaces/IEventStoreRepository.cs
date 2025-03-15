using Inventory.Domain.Events;

namespace Inventory.Domain.Interfaces;

public interface IEventStoreRepository
{
    Task SaveAsync(IDomainEvent domainEvent);
}
