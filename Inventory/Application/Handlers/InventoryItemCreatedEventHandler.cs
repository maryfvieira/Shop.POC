using Inventory.Application.Events;
using Inventory.Application.Interfaces;
using Inventory.Domain.Interfaces;

namespace Inventory.Application.Handlers;

public class InventoryItemCreatedEventHandler : IEventHandler<InventoryItemCreatedEvent>
{
    private readonly IEventStoreRepository _eventStoreRepository;

    public InventoryItemCreatedEventHandler(IEventStoreRepository eventStoreRepository)
    {
        _eventStoreRepository = eventStoreRepository;
    }

    public async Task Handle(InventoryItemCreatedEvent domainEvent)
    {
        await _eventStoreRepository.SaveAsync(domainEvent);
    }
}
